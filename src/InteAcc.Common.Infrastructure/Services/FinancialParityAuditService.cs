using Dapper;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Data;
using InteAcc.Common.Application.Abstractions.Migration;
using InteAcc.Common.Application.Models.Migration;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class FinancialParityAuditService : IFinancialParityAuditService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public FinancialParityAuditService(
        InteAccDbContext dbContext,
        ISqlConnectionFactory sqlConnectionFactory)
    {
        _dbContext = dbContext;
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<FinancialParityAuditReport> RunFinancialParityAuditAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        var companyName = company?.CompanyName ?? "InteAcc Global Holdings Ltd.";

        using var connection = _sqlConnectionFactory.CreateConnection();

        // 1. General Ledger Parity
        var glStatsSql = @"
            SELECT 
                COALESCE(SUM(l.Debit), 0.0000) AS TotalDebits,
                COALESCE(SUM(l.Credit), 0.0000) AS TotalCredits,
                COUNT(DISTINCT v.Id) AS TotalPostedVouchers,
                COUNT(l.Id) AS TotalVoucherLines
            FROM gl.JournalVouchers v
            INNER JOIN gl.JournalVoucherLines l ON l.VoucherId = v.Id
            WHERE v.CompanyId = @CompanyId 
              AND v.IsPosted = 1 
              AND v.IsDeleted = 0 
              AND l.IsDeleted = 0;";

        var glResult = await connection.QuerySingleAsync(glStatsSql, new { CompanyId = companyId });
        decimal glDebits = glResult.TotalDebits;
        decimal glCredits = glResult.TotalCredits;
        decimal glVariance = glDebits - glCredits;

        var report = new FinancialParityAuditReport
        {
            CompanyId = companyId,
            CompanyName = companyName,
            AuditTimestamp = DateTime.UtcNow,
            GeneralLedgerParity = new GeneralLedgerParityDto
            {
                TotalDebits = glDebits,
                TotalCredits = glCredits,
                Variance = glVariance,
                TotalPostedVouchers = glResult.TotalPostedVouchers,
                TotalVoucherLines = glResult.TotalVoucherLines
            }
        };

        // Helper to query GL net account balance for given account codes
        async Task<decimal> GetGlBalanceAsync(string[] accountCodes, bool isCreditNormal = false)
        {
            var sql = @"
                SELECT COALESCE(SUM(CASE WHEN @IsCreditNormal = 1 THEN l.Credit - l.Debit ELSE l.Debit - l.Credit END), 0.0000)
                FROM gl.JournalVouchers v
                INNER JOIN gl.JournalVoucherLines l ON l.VoucherId = v.Id
                INNER JOIN gl.Accounts a ON l.AccountId = a.Id
                WHERE v.CompanyId = @CompanyId 
                  AND v.IsPosted = 1 
                  AND v.IsDeleted = 0 
                  AND l.IsDeleted = 0
                  AND a.AccountCode IN @AccountCodes;";

            return await connection.ExecuteScalarAsync<decimal>(sql, new { CompanyId = companyId, AccountCodes = accountCodes, IsCreditNormal = isCreditNormal });
        }

        // 2. AR Subledger Reconciliation (Account 1130)
        var arGlBalance = await GetGlBalanceAsync(new[] { "1130" }, isCreditNormal: false);
        var arSubledgerSql = "SELECT COALESCE(SUM(BalanceAmount), 0.0000) FROM ar.Invoices WHERE CompanyId = @CompanyId AND IsDeleted = 0;";
        var arSubledgerTotal = await connection.ExecuteScalarAsync<decimal>(arSubledgerSql, new { CompanyId = companyId });
        report.SubledgerReconciliations.Add(new SubledgerParityItem
        {
            SubledgerName = "Accounts Receivable (Trade Debtors)",
            ControlAccountCodes = "1130",
            ControlAccountBalance = arGlBalance,
            SubledgerTotal = arSubledgerTotal,
            Variance = arGlBalance - arSubledgerTotal,
            Remarks = "Matched Customer open invoice balances against GL control account 1130."
        });

        // 3. AP Subledger Reconciliation (Account 2110)
        var apGlBalance = await GetGlBalanceAsync(new[] { "2110" }, isCreditNormal: true);
        var apSubledgerSql = "SELECT COALESCE(SUM(BalanceAmount), 0.0000) FROM ap.PurchaseBills WHERE CompanyId = @CompanyId AND IsDeleted = 0;";
        var apSubledgerTotal = await connection.ExecuteScalarAsync<decimal>(apSubledgerSql, new { CompanyId = companyId });
        report.SubledgerReconciliations.Add(new SubledgerParityItem
        {
            SubledgerName = "Accounts Payable (Trade Creditors)",
            ControlAccountCodes = "2110",
            ControlAccountBalance = apGlBalance,
            SubledgerTotal = apSubledgerTotal,
            Variance = apGlBalance - apSubledgerTotal,
            Remarks = "Matched Vendor open bill balances against GL control account 2110."
        });

        // 4. Inventory Subledger Reconciliation (Accounts 1140 & 1145)
        var invGlBalance = await GetGlBalanceAsync(new[] { "1140", "1145" }, isCreditNormal: false);
        var invSubledgerSql = @"
            SELECT COALESCE(SUM(CASE 
                WHEN TransactionType IN (1, 3, 5) THEN TotalCost 
                WHEN TransactionType IN (2, 4) THEN -TotalCost 
                ELSE 0.0000 END), 0.0000)
            FROM inv.StockLedgers 
            WHERE CompanyId = @CompanyId AND IsDeleted = 0;";
        var invSubledgerTotal = await connection.ExecuteScalarAsync<decimal>(invSubledgerSql, new { CompanyId = companyId });
        report.SubledgerReconciliations.Add(new SubledgerParityItem
        {
            SubledgerName = "Inventory & Stock Valuation",
            ControlAccountCodes = "1140, 1145",
            ControlAccountBalance = invGlBalance,
            SubledgerTotal = invSubledgerTotal,
            Variance = invGlBalance - invSubledgerTotal,
            Remarks = "Reconciled warehouse stock ledger movements against GL inventory control accounts."
        });

        // 5. Cash & Bank Subledger Reconciliation (Accounts 1111 & 1120)
        var bankGlBalance = await GetGlBalanceAsync(new[] { "1111", "1120" }, isCreditNormal: false);
        var bankSubledgerSql = @"
            SELECT 
                (SELECT COALESCE(SUM(CurrentBalance), 0.0000) FROM bc.BankAccounts WHERE CompanyId = @CompanyId AND IsDeleted = 0) +
                (SELECT COALESCE(SUM(CurrentBalance), 0.0000) FROM bc.CashBooks WHERE CompanyId = @CompanyId AND IsDeleted = 0);";
        var bankSubledgerTotal = await connection.ExecuteScalarAsync<decimal>(bankSubledgerSql, new { CompanyId = companyId });
        report.SubledgerReconciliations.Add(new SubledgerParityItem
        {
            SubledgerName = "Cash & Bank Balances",
            ControlAccountCodes = "1111, 1120",
            ControlAccountBalance = bankGlBalance,
            SubledgerTotal = bankSubledgerTotal,
            Variance = bankGlBalance - bankSubledgerTotal,
            Remarks = "Matched physical Cash Books and Bank Account balances against GL cash/bank accounts."
        });

        // 6. Real Estate Customer Booking Advances Reconciliation (Account 2150)
        var reGlBalance = await GetGlBalanceAsync(new[] { "2150" }, isCreditNormal: true);
        var reSubledgerSql = "SELECT COALESCE(SUM(TotalPaid), 0.0000) FROM re.PropertyBookings WHERE CompanyId = @CompanyId AND IsDeleted = 0;";
        var reSubledgerTotal = await connection.ExecuteScalarAsync<decimal>(reSubledgerSql, new { CompanyId = companyId });
        report.SubledgerReconciliations.Add(new SubledgerParityItem
        {
            SubledgerName = "Real Estate Customer Booking Advances",
            ControlAccountCodes = "2150",
            ControlAccountBalance = reGlBalance,
            SubledgerTotal = reSubledgerTotal,
            Variance = reGlBalance - reSubledgerTotal,
            Remarks = "Matched Real Estate customer booking deposits and progress installments against GL account 2150."
        });

        // 7. Fixed Assets Depreciation Reconciliation (Account 1290)
        var faDeprGlBalance = await GetGlBalanceAsync(new[] { "1290" }, isCreditNormal: true);
        var faSubledgerSql = "SELECT COALESCE(SUM(AccumulatedDepreciation), 0.0000) FROM fa.Assets WHERE CompanyId = @CompanyId AND IsDisposed = 0 AND IsDeleted = 0;";
        var faSubledgerTotal = await connection.ExecuteScalarAsync<decimal>(faSubledgerSql, new { CompanyId = companyId });
        report.SubledgerReconciliations.Add(new SubledgerParityItem
        {
            SubledgerName = "Fixed Assets Accumulated Depreciation",
            ControlAccountCodes = "1290",
            ControlAccountBalance = faDeprGlBalance,
            SubledgerTotal = faSubledgerTotal,
            Variance = faDeprGlBalance - faSubledgerTotal,
            Remarks = "Matched active Fixed Asset catalog accumulated depreciation against GL account 1290."
        });

        report.AuditSummaryMessage = report.IsSystemParityAchieved
            ? "ALL AUDIT CRITERIA PASSED: Exact $0.0000 variance verified across General Ledger and all operational subledgers."
            : $"AUDIT DISCREPANCY DETECTED: {report.CriticalDiscrepanciesCount} criteria failed zero-variance threshold.";

        return report;
    }
}
