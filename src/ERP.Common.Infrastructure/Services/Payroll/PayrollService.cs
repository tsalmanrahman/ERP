using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.Payroll;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Payroll;
using ERP.Common.Domain.Entities.HumanResources;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Payroll;

public class PayrollService : IPayrollService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public PayrollService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<PayrollPeriodDto>> GetPayrollPeriodsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var periods = await _dbContext.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new PayrollPeriodDto(
                p.Id,
                p.CompanyId,
                p.PeriodName,
                p.Year,
                p.Month,
                p.StartDate,
                p.EndDate,
                p.WorkingDays,
                p.IsClosed
            ))
            .ToListAsync(cancellationToken);

        return periods;
    }

    public async Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var exists = await _dbContext.PayrollPeriods
            .AnyAsync(p => p.CompanyId == companyId && p.Year == request.Year && p.Month == request.Month, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Payroll period for {request.Year}-{request.Month:D2} already exists.");
        }

        var period = new PayrollPeriod
        {
            CompanyId = companyId,
            PeriodName = request.PeriodName.Trim(),
            Year = request.Year,
            Month = request.Month,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            WorkingDays = request.WorkingDays <= 0 ? 26 : request.WorkingDays,
            IsClosed = false
        };

        _dbContext.PayrollPeriods.Add(period);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PayrollPeriodDto(
            period.Id,
            period.CompanyId,
            period.PeriodName,
            period.Year,
            period.Month,
            period.StartDate,
            period.EndDate,
            period.WorkingDays,
            period.IsClosed
        );
    }

    public async Task<SalarySheetDto> GenerateSalarySheetAsync(GenerateSalarySheetRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var period = await _dbContext.PayrollPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == request.PayrollPeriodId, cancellationToken);

        if (period == null)
        {
            throw new InvalidOperationException($"Payroll period with ID {request.PayrollPeriodId} was not found.");
        }

        if (period.IsClosed)
        {
            throw new InvalidOperationException($"Payroll period '{period.PeriodName}' is closed.");
        }

        var existingSheet = await _dbContext.SalarySheets
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.PayrollPeriodId == period.Id, cancellationToken);

        if (existingSheet != null && existingSheet.Status != "Draft")
        {
            throw new InvalidOperationException($"Salary sheet for period '{period.PeriodName}' is already {existingSheet.Status} and cannot be regenerated.");
        }

        // Active employees
        var employees = await _dbContext.Employees
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .ToListAsync(cancellationToken);

        // Daily attendance for this period
        var attendances = await _dbContext.DailyAttendance
            .Where(d => d.CompanyId == companyId && d.AttendanceDate >= period.StartDate && d.AttendanceDate <= period.EndDate)
            .ToListAsync(cancellationToken);

        var sheetLines = new List<SalarySheetLine>();

        foreach (var emp in employees)
        {
            var empAtt = attendances.Where(a => a.EmployeeId == emp.Id).ToList();

            int presentDays = empAtt.Count(a => a.Status == "Present");
            int lateDays = empAtt.Count(a => a.Status == "Late");
            int absentDays = empAtt.Count(a => a.Status == "Absent");
            decimal overtimeHours = empAtt.Sum(a => a.OvertimeHours);

            var line = new SalarySheetLine
            {
                EmployeeId = emp.Id,
                BasicSalary = emp.BasicSalary,
                HouseRent = emp.HouseRent,
                MedicalAllowance = emp.MedicalAllowance,
                ConveyanceAllowance = emp.ConveyanceAllowance,
                OtherAllowance = emp.OtherAllowance,
                WorkingDays = period.WorkingDays,
                PresentDays = presentDays,
                LateDays = lateDays,
                AbsentDays = absentDays,
                OvertimeHours = overtimeHours,
                TaxDeduction = emp.TaxDeduction,
                ProvidentFundDeduction = emp.ProvidentFundDeduction,
                PaymentMethod = "BankTransfer",
                BankName = emp.BankName ?? "Prime Bank Ltd.",
                BankAccountNumber = emp.BankAccountNumber,
                Remarks = $"Present: {presentDays}, Late: {lateDays}, Absent: {absentDays}, OT: {overtimeHours}h"
            };

            line.Calculate();
            sheetLines.Add(line);
        }

        SalarySheet sheet;
        if (existingSheet != null)
        {
            // Remove previous lines and update
            _dbContext.SalarySheetLines.RemoveRange(existingSheet.Lines);
            existingSheet.Lines = sheetLines;
            existingSheet.ProcessingDate = request.ProcessingDate?.Date ?? DateTime.UtcNow.Date;
            existingSheet.RecalculateTotals();
            sheet = existingSheet;
        }
        else
        {
            var sheetNumber = $"SAL-{period.Year}{period.Month:D2}-0001";
            sheet = new SalarySheet
            {
                CompanyId = companyId,
                PayrollPeriodId = period.Id,
                SheetNumber = sheetNumber,
                ProcessingDate = request.ProcessingDate?.Date ?? DateTime.UtcNow.Date,
                Status = "Draft",
                Lines = sheetLines
            };
            sheet.RecalculateTotals();
            _dbContext.SalarySheets.Add(sheet);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSalarySheetByIdAsync(sheet.Id, cancellationToken))!;
    }

    public async Task<SalarySheetDto?> GetSalarySheetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var sheet = await _dbContext.SalarySheets
            .Include(s => s.PayrollPeriod)
            .Include(s => s.Lines)
                .ThenInclude(l => l.Employee!)
                    .ThenInclude(e => e.Department)
            .Include(s => s.Lines)
                .ThenInclude(l => l.Employee!)
                    .ThenInclude(e => e.Designation)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken);

        if (sheet == null) return null;

        var accrualJv = sheet.AccrualJournalVoucherId.HasValue
            ? await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(j => j.Id == sheet.AccrualJournalVoucherId.Value, cancellationToken)
            : null;

        var disbJv = sheet.DisbursementJournalVoucherId.HasValue
            ? await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(j => j.Id == sheet.DisbursementJournalVoucherId.Value, cancellationToken)
            : null;

        return MapSheetToDto(sheet, accrualJv?.VoucherNumber, disbJv?.VoucherNumber);
    }

    public async Task<SalarySheetDto?> GetSalarySheetByPeriodIdAsync(int periodId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var sheet = await _dbContext.SalarySheets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.PayrollPeriodId == periodId, cancellationToken);

        if (sheet == null) return null;
        return await GetSalarySheetByIdAsync(sheet.Id, cancellationToken);
    }

    public async Task<SalarySheetDto> ApproveSalarySheetAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var sheet = await _dbContext.SalarySheets
            .Include(s => s.PayrollPeriod)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken);

        if (sheet == null)
        {
            throw new InvalidOperationException($"Salary sheet with ID {id} was not found.");
        }

        if (sheet.Status != "Draft")
        {
            throw new InvalidOperationException($"Salary sheet {sheet.SheetNumber} is already {sheet.Status}.");
        }

        // GL Accounts:
        // 5210 - Salaries, Wages & Benefits (Expense)
        // 2120 - Accrued Salaries & Expenses Payable (Liability)
        // 2130 - VAT & Tax Withholding Payable (Liability)
        var salExpenseAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "5210", cancellationToken);
        var accruedSalPayableAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2120", cancellationToken);
        var taxPayableAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2130", cancellationToken);

        if (salExpenseAccount == null || accruedSalPayableAccount == null || taxPayableAccount == null)
        {
            throw new InvalidOperationException("One or more required GL accounts (5210, 2120, 2130) were not found.");
        }

        decimal totalGrossAndOt = sheet.TotalGrossSalary + sheet.TotalOvertimeAmount;
        decimal totalAbsentDeduction = sheet.Lines.Sum(l => l.AbsentDeduction);
        decimal totalTaxDeduction = sheet.Lines.Sum(l => l.TaxDeduction);
        decimal totalPfDeduction = sheet.Lines.Sum(l => l.ProvidentFundDeduction);

        // Effective Salary Expense = (Gross + Overtime) - AbsentDeductions
        decimal netSalaryExpense = totalGrossAndOt - totalAbsentDeduction;

        // Total Accrued Salaries Payable = NetSalaryPayable + PfDeduction
        decimal accruedPayableAmount = sheet.TotalNetSalary + totalPfDeduction;

        var jvLines = new List<CreateVoucherLineRequest>
        {
            new(
                AccountId: salExpenseAccount.Id,
                Debit: netSalaryExpense,
                Credit: 0m,
                Particulars: $"Payroll Expense Accrual for {sheet.PayrollPeriod?.PeriodName ?? sheet.SheetNumber}"
            )
        };

        if (totalTaxDeduction > 0)
        {
            jvLines.Add(new(
                AccountId: taxPayableAccount.Id,
                Debit: 0m,
                Credit: totalTaxDeduction,
                Particulars: $"Payroll Tax Withholding for {sheet.PayrollPeriod?.PeriodName ?? sheet.SheetNumber}"
            ));
        }

        jvLines.Add(new(
            AccountId: accruedSalPayableAccount.Id,
            Debit: 0m,
            Credit: accruedPayableAmount,
            Particulars: $"Accrued Salaries & Benefits Payable for {sheet.PayrollPeriod?.PeriodName ?? sheet.SheetNumber}"
        ));

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(sheet.ProcessingDate),
            Reference: sheet.SheetNumber,
            Description: $"Monthly Salary Accrual: {sheet.SheetNumber} ({sheet.PayrollPeriod?.PeriodName})",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create salary accrual journal voucher: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post salary accrual journal voucher: {postResult.Error}");
        }

        sheet.Status = "Approved";
        sheet.AccrualJournalVoucherId = createResult.Value!.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSalarySheetByIdAsync(sheet.Id, cancellationToken))!;
    }

    public async Task<SalarySheetDto> DisburseSalarySheetAsync(DisburseSalaryRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var sheet = await _dbContext.SalarySheets
            .Include(s => s.PayrollPeriod)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == request.SalarySheetId, cancellationToken);

        if (sheet == null)
        {
            throw new InvalidOperationException($"Salary sheet with ID {request.SalarySheetId} was not found.");
        }

        if (sheet.Status != "Approved")
        {
            throw new InvalidOperationException($"Salary sheet {sheet.SheetNumber} is in status '{sheet.Status}' and cannot be disbursed. Only 'Approved' sheets can be disbursed.");
        }

        // Resolving GL Accounts:
        // Debit: 2120 Accrued Salaries & Expenses Payable
        // Credit: 1120 Prime Bank Ltd. (or specified bank account)
        var accruedSalPayableAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2120", cancellationToken);

        var bankAccount = request.BankAccountId.HasValue
            ? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == request.BankAccountId.Value, cancellationToken)
            : await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1120", cancellationToken);

        if (accruedSalPayableAccount == null || bankAccount == null)
        {
            throw new InvalidOperationException("Accrued Salaries Payable (2120) or Disbursing Bank Account (1120) was not found.");
        }

        var disburseDate = request.DisbursementDate?.Date ?? DateTime.UtcNow.Date;
        var disburseAmount = sheet.TotalNetSalary;

        var jvLines = new List<CreateVoucherLineRequest>
        {
            new(
                AccountId: accruedSalPayableAccount.Id,
                Debit: disburseAmount,
                Credit: 0m,
                Particulars: $"Salary Disbursement for {sheet.PayrollPeriod?.PeriodName ?? sheet.SheetNumber}"
            ),
            new(
                AccountId: bankAccount.Id,
                Debit: 0m,
                Credit: disburseAmount,
                Particulars: $"Bank transfer via {bankAccount.AccountName} ({request.ChequeOrReferenceNumber ?? sheet.SheetNumber})"
            )
        };

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(disburseDate),
            Reference: request.ChequeOrReferenceNumber ?? sheet.SheetNumber,
            Description: $"Salary Disbursement: {sheet.SheetNumber} via {bankAccount.AccountName}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create salary disbursement journal voucher: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post salary disbursement journal voucher: {postResult.Error}");
        }

        sheet.Status = "Disbursed";
        sheet.DisbursementJournalVoucherId = createResult.Value!.Id;

        // Close period if present
        if (sheet.PayrollPeriod != null)
        {
            sheet.PayrollPeriod.IsClosed = true;
            sheet.PayrollPeriod.ClosedAtUtc = DateTime.UtcNow;
            sheet.PayrollPeriod.ClosedBy = _currentUserContext.UserName ?? "System";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSalarySheetByIdAsync(sheet.Id, cancellationToken))!;
    }

    private static SalarySheetDto MapSheetToDto(SalarySheet s, string? accrualJvNumber, string? disbJvNumber)
    {
        var linesDto = s.Lines.Select(l => new SalarySheetLineDto(
            l.Id,
            l.SalarySheetId,
            l.EmployeeId,
            l.Employee?.EmployeeCode ?? string.Empty,
            l.Employee?.FullName ?? string.Empty,
            l.Employee?.Designation?.DesignationTitle ?? string.Empty,
            l.Employee?.Department?.DepartmentName ?? string.Empty,
            l.BasicSalary,
            l.HouseRent,
            l.MedicalAllowance,
            l.ConveyanceAllowance,
            l.OtherAllowance,
            l.GrossSalary,
            l.WorkingDays,
            l.PresentDays,
            l.LateDays,
            l.AbsentDays,
            l.OvertimeHours,
            l.OvertimeRate,
            l.OvertimeAmount,
            l.AbsentDeduction,
            l.TaxDeduction,
            l.ProvidentFundDeduction,
            l.TotalDeductions,
            l.NetSalaryPayable,
            l.PaymentMethod,
            l.BankName,
            l.BankAccountNumber,
            l.Remarks
        )).ToList();

        return new SalarySheetDto(
            s.Id,
            s.CompanyId,
            s.PayrollPeriodId,
            s.PayrollPeriod?.PeriodName ?? string.Empty,
            s.SheetNumber,
            s.ProcessingDate,
            s.TotalGrossSalary,
            s.TotalOvertimeAmount,
            s.TotalDeductions,
            s.TotalNetSalary,
            s.Status,
            s.AccrualJournalVoucherId,
            accrualJvNumber,
            s.DisbursementJournalVoucherId,
            disbJvNumber,
            linesDto
        );
    }
}
