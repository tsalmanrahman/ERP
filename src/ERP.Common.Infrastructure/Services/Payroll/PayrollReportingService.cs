using Dapper;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.Payroll;
using ERP.Common.Application.Models.Payroll;

namespace ERP.Common.Infrastructure.Services.Payroll;

public class PayrollReportingService : IPayrollReportingService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICurrentUserContext _currentUserContext;

    public PayrollReportingService(
        ISqlConnectionFactory sqlConnectionFactory,
        ICurrentUserContext currentUserContext)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _currentUserContext = currentUserContext;
    }

    private int GetCompanyId() => _currentUserContext.GetRequiredCompanyId();

    public async Task<PaySlipDto?> GetPaySlipAsync(int salarySheetId, int employeeId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                l.EmployeeId,
                e.EmployeeCode,
                CONCAT(e.FirstName, ' ', e.LastName) AS EmployeeName,
                des.DesignationTitle,
                d.DepartmentName,
                e.JoiningDate,
                ISNULL(l.BankName, e.BankName) AS BankName,
                ISNULL(l.BankAccountNumber, e.BankAccountNumber) AS BankAccountNumber,
                p.PeriodName,
                p.Year,
                p.Month,
                s.ProcessingDate,
                l.WorkingDays,
                l.PresentDays,
                l.LateDays,
                l.AbsentDays,
                l.OvertimeHours,
                l.BasicSalary,
                l.HouseRent,
                l.MedicalAllowance,
                l.ConveyanceAllowance,
                l.OtherAllowance,
                l.OvertimeRate,
                l.OvertimeAmount,
                (l.GrossSalary + l.OvertimeAmount) AS TotalGrossEarnings,
                l.AbsentDeduction,
                l.TaxDeduction,
                l.ProvidentFundDeduction,
                l.TotalDeductions,
                l.NetSalaryPayable
            FROM hr.SalarySheetLines l
            INNER JOIN hr.SalarySheets s ON l.SalarySheetId = s.Id
            INNER JOIN hr.PayrollPeriods p ON s.PayrollPeriodId = p.Id
            INNER JOIN hr.Employees e ON l.EmployeeId = e.Id
            LEFT JOIN hr.Departments d ON e.DepartmentId = d.Id
            LEFT JOIN hr.Designations des ON e.DesignationId = des.Id
            WHERE s.CompanyId = @CompanyId 
              AND s.Id = @SalarySheetId 
              AND l.EmployeeId = @EmployeeId;";

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, new { CompanyId = companyId, SalarySheetId = salarySheetId, EmployeeId = employeeId }, cancellationToken: cancellationToken));

        if (row == null) return null;

        return new PaySlipDto(
            EmployeeId: (int)row.EmployeeId,
            EmployeeCode: (string)row.EmployeeCode,
            EmployeeName: (string)row.EmployeeName,
            DesignationTitle: (string?)row.DesignationTitle ?? string.Empty,
            DepartmentName: (string?)row.DepartmentName ?? string.Empty,
            JoiningDate: (DateTime)row.JoiningDate,
            BankName: (string?)row.BankName,
            BankAccountNumber: (string?)row.BankAccountNumber,
            PeriodName: (string)row.PeriodName,
            Year: (int)row.Year,
            Month: (int)row.Month,
            ProcessingDate: (DateTime)row.ProcessingDate,
            WorkingDays: (int)row.WorkingDays,
            PresentDays: (int)row.PresentDays,
            LateDays: (int)row.LateDays,
            AbsentDays: (int)row.AbsentDays,
            OvertimeHours: (decimal)row.OvertimeHours,
            BasicSalary: (decimal)row.BasicSalary,
            HouseRent: (decimal)row.HouseRent,
            MedicalAllowance: (decimal)row.MedicalAllowance,
            ConveyanceAllowance: (decimal)row.ConveyanceAllowance,
            OtherAllowance: (decimal)row.OtherAllowance,
            OvertimeRate: (decimal)row.OvertimeRate,
            OvertimeAmount: (decimal)row.OvertimeAmount,
            TotalGrossEarnings: (decimal)row.TotalGrossEarnings,
            AbsentDeduction: (decimal)row.AbsentDeduction,
            TaxDeduction: (decimal)row.TaxDeduction,
            ProvidentFundDeduction: (decimal)row.ProvidentFundDeduction,
            TotalDeductions: (decimal)row.TotalDeductions,
            NetSalaryPayable: (decimal)row.NetSalaryPayable
        );
    }

    public async Task<BankAdviceDto?> GetBankAdviceAsync(int salarySheetId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string headerSql = @"
            SELECT 
                p.PeriodName,
                s.ProcessingDate,
                s.SheetNumber,
                s.TotalNetSalary,
                (SELECT COUNT(*) FROM hr.SalarySheetLines WHERE SalarySheetId = s.Id) AS TotalEmployees
            FROM hr.SalarySheets s
            INNER JOIN hr.PayrollPeriods p ON s.PayrollPeriodId = p.Id
            WHERE s.CompanyId = @CompanyId AND s.Id = @SalarySheetId;";

        var header = await connection.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(headerSql, new { CompanyId = companyId, SalarySheetId = salarySheetId }, cancellationToken: cancellationToken));

        if (header == null) return null;

        const string linesSql = @"
            SELECT 
                e.EmployeeCode,
                CONCAT(e.FirstName, ' ', e.LastName) AS EmployeeName,
                ISNULL(l.BankName, e.BankName) AS BankName,
                ISNULL(l.BankAccountNumber, e.BankAccountNumber) AS BankAccountNumber,
                l.NetSalaryPayable
            FROM hr.SalarySheetLines l
            INNER JOIN hr.Employees e ON l.EmployeeId = e.Id
            WHERE l.SalarySheetId = @SalarySheetId
            ORDER BY e.EmployeeCode;";

        var lines = await connection.QueryAsync<dynamic>(
            new CommandDefinition(linesSql, new { SalarySheetId = salarySheetId }, cancellationToken: cancellationToken));

        var items = lines.Select(r => new BankAdviceItemDto(
            EmployeeCode: (string)r.EmployeeCode,
            EmployeeName: (string)r.EmployeeName,
            BankName: (string?)r.BankName,
            BankAccountNumber: (string?)r.BankAccountNumber,
            NetSalaryPayable: (decimal)r.NetSalaryPayable
        )).ToList();

        return new BankAdviceDto(
            PeriodName: (string)header.PeriodName,
            ProcessingDate: (DateTime)header.ProcessingDate,
            SheetNumber: (string)header.SheetNumber,
            TotalAmount: (decimal)header.TotalNetSalary,
            TotalEmployees: (int)header.TotalEmployees,
            Items: items
        );
    }
}
