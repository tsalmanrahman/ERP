using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Domain.Common;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Finance;

public class JournalVoucherService : IJournalVoucherService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDocumentSequenceService? _documentSequenceService;

    public JournalVoucherService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IDocumentSequenceService? documentSequenceService = null)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _documentSequenceService = documentSequenceService;
    }

    public async Task<Result<JournalVoucherDto>> CreateVoucherAsync(CreateJournalVoucherRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.CompanyId ?? 1;

        if (request.Lines == null || request.Lines.Count < 2)
        {
            return Result.Failure<JournalVoucherDto>("A journal voucher requires at least two line items (debit and credit).");
        }

        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);

        if (Math.Abs(totalDebit - totalCredit) > 0.0001m)
        {
            return Result.Failure<JournalVoucherDto>($"Double-entry invariant failed: Debits ({totalDebit:F2}) and Credits ({totalCredit:F2}) must be exactly equal.");
        }

        if (totalDebit <= 0)
        {
            return Result.Failure<JournalVoucherDto>("Total voucher amount must be greater than zero.");
        }

        // Verify accounts exist and belong to this company
        var requestedAccountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var validAccounts = await _dbContext.Accounts
            .Where(a => a.CompanyId == companyId && requestedAccountIds.Contains(a.Id) && a.IsActive)
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        if (validAccounts.Count != requestedAccountIds.Count)
        {
            return Result.Failure<JournalVoucherDto>("One or more selected accounts are invalid or inactive for this company.");
        }

        // Find active fiscal period
        var voucherDate = request.VoucherDate.ToDateTime(TimeOnly.MinValue);
        var fiscalPeriod = await _dbContext.Database.SqlQueryRaw<FiscalPeriodInfo>(@"
            SELECT TOP 1 fp.Id AS FiscalPeriodId, fy.Id AS FiscalYearId, fp.IsClosed
            FROM sa.FiscalPeriods fp
            INNER JOIN sa.FiscalYears fy ON fp.FiscalYearId = fy.Id
            WHERE fy.CompanyId = {0} AND {1} BETWEEN fp.StartDate AND fp.EndDate",
            companyId, voucherDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (fiscalPeriod == null)
        {
            // Fallback to first open period if no exact date match
            fiscalPeriod = await _dbContext.Database.SqlQueryRaw<FiscalPeriodInfo>(@"
                SELECT TOP 1 fp.Id AS FiscalPeriodId, fy.Id AS FiscalYearId, fp.IsClosed
                FROM sa.FiscalPeriods fp
                INNER JOIN sa.FiscalYears fy ON fp.FiscalYearId = fy.Id
                WHERE fy.CompanyId = {0} AND fp.IsClosed = 0
                ORDER BY fp.PeriodNumber",
                companyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (fiscalPeriod == null || fiscalPeriod.IsClosed)
        {
            return Result.Failure<JournalVoucherDto>("Cannot create voucher: active fiscal period is closed or undefined.");
        }

        // Generate Voucher Number
        string voucherNumber;
        if (_documentSequenceService != null)
        {
            voucherNumber = await _documentSequenceService.GetNextNumberAsync(
                companyId, "JV", "JV-", request.VoucherDate.ToString("yyyyMM"), 4, cancellationToken);
        }
        else
        {
            var prefix = $"JV-{request.VoucherDate:yyyyMM}";
            var count = await _dbContext.JournalVouchers
                .IgnoreQueryFilters()
                .CountAsync(v => v.CompanyId == companyId && v.VoucherNumber.StartsWith(prefix), cancellationToken);
            voucherNumber = $"{prefix}-{(count + 1):D4}";
        }

        // Create Batch if not exists
        var batch = await _dbContext.Database.SqlQueryRaw<int>(@"
            SELECT TOP 1 Id FROM gl.JournalBatches 
            WHERE CompanyId = {0} AND Status = 0", companyId)
            .ToListAsync(cancellationToken);

        int batchId;
        if (batch.Any())
        {
            batchId = batch.First();
        }
        else
        {
            var batchNum = $"BATCH-{_dateTimeProvider.UtcNow:yyyyMMdd}";
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                INSERT INTO gl.JournalBatches (CompanyId, BatchNumber, BatchDate, SourceModule, Description, Status, TotalDebit, TotalCredit)
                VALUES ({0}, {1}, {2}, 'GL', 'Standard GL Batch', 0, 0, 0)",
                companyId, batchNum, voucherDate);
            batchId = await _dbContext.Database.SqlQueryRaw<int>("SELECT CAST(SCOPE_IDENTITY() AS INT) AS [Value]").FirstAsync(cancellationToken);
        }

        var voucher = new JournalVoucher
        {
            BatchId = batchId,
            CompanyId = companyId,
            VoucherNumber = voucherNumber,
            VoucherDate = request.VoucherDate,
            Reference = request.Reference,
            Description = request.Description,
            FiscalYearId = fiscalPeriod.FiscalYearId,
            FiscalPeriodId = fiscalPeriod.FiscalPeriodId,
            IsPosted = false
        };

        int lineNumber = 1;
        foreach (var lineReq in request.Lines)
        {
            voucher.Lines.Add(new JournalVoucherLine
            {
                LineNumber = lineNumber++,
                AccountId = lineReq.AccountId,
                Debit = lineReq.Debit,
                Credit = lineReq.Credit,
                CurrencyCode = "BDT",
                ExchangeRate = 1m,
                BaseDebit = lineReq.Debit,
                BaseCredit = lineReq.Credit,
                Particulars = lineReq.Particulars
            });
        }

        _dbContext.JournalVouchers.Add(voucher);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetVoucherByIdAsync(voucher.Id, cancellationToken);
    }

    public async Task<Result<JournalVoucherDto>> PostVoucherAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var voucher = await _dbContext.JournalVouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken);

        if (voucher == null)
        {
            return Result.Failure<JournalVoucherDto>("Voucher not found.");
        }

        if (voucher.IsPosted)
        {
            return Result.Failure<JournalVoucherDto>($"Voucher {voucher.VoucherNumber} has already been posted to the General Ledger.");
        }

        if (!voucher.IsBalanced)
        {
            return Result.Failure<JournalVoucherDto>("Cannot post unbalanced voucher.");
        }

        voucher.IsPosted = true;
        voucher.PostedAtUtc = _dateTimeProvider.UtcNow;
        voucher.PostedBy = _currentUserContext.UserName ?? "System";

        // Update real-time Bank and Cash book balances for affected accounts
        foreach (var line in voucher.Lines)
        {
            var netChange = line.Debit - line.Credit;

            // Bank Accounts
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                UPDATE bc.BankAccounts 
                SET CurrentBalance = CurrentBalance + {0}
                WHERE CompanyId = {1} AND GlAccountId = {2}",
                netChange, voucher.CompanyId, line.AccountId);

            // Cash Books
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                UPDATE bc.CashBooks 
                SET CurrentBalance = CurrentBalance + {0}
                WHERE CompanyId = {1} AND GlAccountId = {2}",
                netChange, voucher.CompanyId, line.AccountId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetVoucherByIdAsync(voucher.Id, cancellationToken);
    }

    public async Task<Result<List<JournalVoucherSummaryDto>>> GetVouchersAsync(int? fiscalPeriodId, bool? isPosted, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JournalVouchers.AsNoTracking();

        if (fiscalPeriodId.HasValue)
        {
            query = query.Where(v => v.FiscalPeriodId == fiscalPeriodId.Value);
        }

        if (isPosted.HasValue)
        {
            query = query.Where(v => v.IsPosted == isPosted.Value);
        }

        var vouchers = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.VoucherNumber)
            .Select(v => new JournalVoucherSummaryDto(
                v.Id,
                v.VoucherNumber,
                v.VoucherDate,
                v.Reference,
                v.Description,
                v.IsPosted,
                v.Lines.Sum(l => l.Debit),
                v.Lines.Sum(l => l.Credit),
                v.Lines.Count
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(vouchers);
    }

    public async Task<Result<JournalVoucherDto>> GetVoucherByIdAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var voucher = await _dbContext.JournalVouchers
            .Include(v => v.Lines)
            .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken);

        if (voucher == null)
        {
            return Result.Failure<JournalVoucherDto>("Voucher not found.");
        }

        var linesDto = voucher.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new JournalVoucherLineDto(
                l.Id,
                l.LineNumber,
                l.AccountId,
                l.Account?.AccountCode ?? string.Empty,
                l.Account?.AccountName ?? string.Empty,
                l.Debit,
                l.Credit,
                l.Particulars
            ))
            .ToList();

        var dto = new JournalVoucherDto(
            voucher.Id,
            voucher.CompanyId,
            voucher.VoucherNumber,
            voucher.VoucherDate,
            voucher.Reference,
            voucher.Description,
            voucher.FiscalYearId,
            voucher.FiscalPeriodId,
            voucher.IsPosted,
            voucher.PostedAtUtc,
            voucher.PostedBy,
            voucher.TotalDebit,
            voucher.TotalCredit,
            voucher.IsBalanced,
            linesDto
        );

        return Result.Success(dto);
    }

    private class FiscalPeriodInfo
    {
        public int FiscalPeriodId { get; set; }
        public int FiscalYearId { get; set; }
        public bool IsClosed { get; set; }
    }
}
