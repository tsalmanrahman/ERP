using Dapper;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Domain.Common;

namespace ERP.Common.Infrastructure.Services.Finance;

public class FinancialReportingService : IFinancialReportingService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinancialReportingService(
        ISqlConnectionFactory sqlConnectionFactory,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TrialBalanceReportDto>> GetTrialBalanceAsync(DateOnly? asOfDate, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        var cutoff = asOfDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var cutoffDateTime = cutoff.ToDateTime(TimeOnly.MaxValue);

        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                a.Id AS AccountId,
                a.AccountCode,
                a.AccountName,
                ag.GroupName,
                CASE ag.AccountCategory
                    WHEN 1 THEN 'Asset'
                    WHEN 2 THEN 'Liability'
                    WHEN 3 THEN 'Equity'
                    WHEN 4 THEN 'Revenue'
                    WHEN 5 THEN 'Expense'
                    ELSE 'Other'
                END AS CategoryName,
                ag.AccountCategory,
                ISNULL(SUM(l.BaseDebit), 0) AS TotalDebit,
                ISNULL(SUM(l.BaseCredit), 0) AS TotalCredit
            FROM gl.Accounts a
            INNER JOIN gl.AccountGroups ag ON a.AccountGroupId = ag.Id
            LEFT JOIN gl.JournalVoucherLines l ON a.Id = l.AccountId AND l.IsDeleted = 0
            LEFT JOIN gl.JournalVouchers v ON l.VoucherId = v.Id AND v.CompanyId = @CompanyId AND v.IsPosted = 1 AND v.IsDeleted = 0 AND v.VoucherDate <= @CutoffDate
            WHERE a.CompanyId = @CompanyId AND a.IsDeleted = 0 AND a.IsActive = 1
            GROUP BY a.Id, a.AccountCode, a.AccountName, ag.GroupName, ag.AccountCategory
            ORDER BY a.AccountCode;";

        var rows = await connection.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { CompanyId = companyId, CutoffDate = cutoffDateTime }, cancellationToken: cancellationToken));

        var lines = new List<TrialBalanceLineDto>();
        decimal sumDebits = 0;
        decimal sumCredits = 0;

        foreach (var r in rows)
        {
            decimal debits = (decimal)r.TotalDebit;
            decimal credits = (decimal)r.TotalCredit;
            int category = (int)r.AccountCategory;

            // Only report accounts with non-zero movement
            if (debits == 0 && credits == 0) continue;

            // Calculate net side for Trial Balance
            decimal netDebit = 0;
            decimal netCredit = 0;

            if (category == 1 || category == 5) // Asset or Expense
            {
                if (debits >= credits)
                    netDebit = debits - credits;
                else
                    netCredit = credits - debits;
            }
            else // Liability, Equity, Revenue
            {
                if (credits >= debits)
                    netCredit = credits - debits;
                else
                    netDebit = debits - credits;
            }

            sumDebits += netDebit;
            sumCredits += netCredit;

            lines.Add(new TrialBalanceLineDto(
                AccountId: (int)r.AccountId,
                AccountCode: (string)r.AccountCode,
                AccountName: (string)r.AccountName,
                GroupName: (string)r.GroupName,
                CategoryName: (string)r.CategoryName,
                Debit: netDebit,
                Credit: netCredit
            ));
        }

        var difference = Math.Abs(sumDebits - sumCredits);
        var isBalanced = difference < 0.0001m;

        var report = new TrialBalanceReportDto(
            AsOfDate: cutoff,
            CompanyId: companyId,
            TotalDebit: sumDebits,
            TotalCredit: sumCredits,
            Difference: difference,
            IsBalanced: isBalanced,
            Lines: lines
        );

        return Result.Success(report);
    }

    public async Task<Result<AccountStatementReportDto>> GetAccountStatementAsync(int accountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        using var connection = _sqlConnectionFactory.CreateConnection();

        // 1. Get Account Info
        const string acctSql = @"
            SELECT a.AccountCode, a.AccountName, ag.AccountCategory
            FROM gl.Accounts a
            INNER JOIN gl.AccountGroups ag ON a.AccountGroupId = ag.Id
            WHERE a.Id = @AccountId AND a.CompanyId = @CompanyId AND a.IsDeleted = 0;";

        var acct = await connection.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(acctSql, new { AccountId = accountId, CompanyId = companyId }, cancellationToken: cancellationToken));

        if (acct == null)
        {
            return Result.Failure<AccountStatementReportDto>("Account not found.");
        }

        int category = (int)acct.AccountCategory;
        bool isDebitNormal = (category == 1 || category == 5);

        // 2. Calculate Opening Balance before fromDate
        const string opSql = @"
            SELECT 
                ISNULL(SUM(l.BaseDebit), 0) AS OpenDebit,
                ISNULL(SUM(l.BaseCredit), 0) AS OpenCredit
            FROM gl.JournalVoucherLines l
            INNER JOIN gl.JournalVouchers v ON l.VoucherId = v.Id
            WHERE l.AccountId = @AccountId 
              AND v.CompanyId = @CompanyId 
              AND v.IsPosted = 1 
              AND v.IsDeleted = 0 
              AND v.VoucherDate < @FromDate;";

        var fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);
        var op = await connection.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(opSql, new { AccountId = accountId, CompanyId = companyId, FromDate = fromDateTime }, cancellationToken: cancellationToken));

        decimal openDebit = (decimal)(op?.OpenDebit ?? 0m);
        decimal openCredit = (decimal)(op?.OpenCredit ?? 0m);
        decimal openingBalance = isDebitNormal ? (openDebit - openCredit) : (openCredit - openDebit);

        // 3. Transactions in period
        const string tranSql = @"
            SELECT 
                v.VoucherDate,
                v.VoucherNumber,
                v.Reference,
                ISNULL(l.Particulars, v.Description) AS Particulars,
                l.BaseDebit AS Debit,
                l.BaseCredit AS Credit
            FROM gl.JournalVoucherLines l
            INNER JOIN gl.JournalVouchers v ON l.VoucherId = v.Id
            WHERE l.AccountId = @AccountId 
              AND v.CompanyId = @CompanyId 
              AND v.IsPosted = 1 
              AND v.IsDeleted = 0 
              AND v.VoucherDate >= @FromDate 
              AND v.VoucherDate <= @ToDate
            ORDER BY v.VoucherDate, v.VoucherNumber, l.LineNumber;";

        var toDateTime = toDate.ToDateTime(TimeOnly.MaxValue);
        var trans = await connection.QueryAsync<dynamic>(
            new CommandDefinition(tranSql, new { AccountId = accountId, CompanyId = companyId, FromDate = fromDateTime, ToDate = toDateTime }, cancellationToken: cancellationToken));

        var statementLines = new List<AccountStatementLineDto>();
        decimal runningBalance = openingBalance;
        decimal totalDebit = 0;
        decimal totalCredit = 0;

        foreach (var t in trans)
        {
            decimal d = (decimal)t.Debit;
            decimal c = (decimal)t.Credit;
            totalDebit += d;
            totalCredit += c;

            runningBalance += isDebitNormal ? (d - c) : (c - d);

            statementLines.Add(new AccountStatementLineDto(
                Date: DateOnly.FromDateTime((DateTime)t.VoucherDate),
                VoucherNumber: (string)t.VoucherNumber,
                Reference: (string?)t.Reference,
                Particulars: (string?)t.Particulars,
                Debit: d,
                Credit: c,
                RunningBalance: runningBalance
            ));
        }

        var closingBalance = runningBalance;

        var dto = new AccountStatementReportDto(
            AccountId: accountId,
            AccountCode: (string)acct.AccountCode,
            AccountName: (string)acct.AccountName,
            FromDate: fromDate,
            ToDate: toDate,
            OpeningBalance: openingBalance,
            TotalDebit: totalDebit,
            TotalCredit: totalCredit,
            ClosingBalance: closingBalance,
            Transactions: statementLines
        );

        return Result.Success(dto);
    }
}
