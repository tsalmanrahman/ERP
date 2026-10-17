using Dapper;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.ProcureToPay;
using ERP.Common.Application.Models.ProcureToPay;

namespace ERP.Common.Infrastructure.Services.ProcureToPay;

public class AccountsPayableReportingService : IAccountsPayableReportingService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AccountsPayableReportingService(
        ISqlConnectionFactory sqlConnectionFactory,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<VendorAgingReport> GetVendorAgingReportAsync(
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var asOf = asOfDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var asOfDateTime = asOf.ToDateTime(TimeOnly.MinValue);

        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                v.Id AS VendorId,
                v.VendorCode,
                v.VendorName,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, b.DueDate, @AsOfDate) <= 30 THEN b.BalanceAmount ELSE 0 END), 0) AS CurrentDays,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, b.DueDate, @AsOfDate) BETWEEN 31 AND 60 THEN b.BalanceAmount ELSE 0 END), 0) AS ThirtyToSixty,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, b.DueDate, @AsOfDate) BETWEEN 61 AND 90 THEN b.BalanceAmount ELSE 0 END), 0) AS SixtyToNinety,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, b.DueDate, @AsOfDate) BETWEEN 91 AND 120 THEN b.BalanceAmount ELSE 0 END), 0) AS NinetyToOneTwenty,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, b.DueDate, @AsOfDate) > 120 THEN b.BalanceAmount ELSE 0 END), 0) AS OverOneTwenty,
                ISNULL(SUM(b.BalanceAmount), 0) AS TotalOutstanding
            FROM ap.Vendors v
            LEFT JOIN ap.PurchaseBills b ON v.Id = b.VendorId AND b.CompanyId = @CompanyId AND b.BalanceAmount > 0 AND b.IsDeleted = 0
            WHERE v.CompanyId = @CompanyId AND v.IsDeleted = 0
            GROUP BY v.Id, v.VendorCode, v.VendorName, v.CurrentBalance
            HAVING ISNULL(SUM(b.BalanceAmount), 0) > 0 OR v.CurrentBalance > 0
            ORDER BY TotalOutstanding DESC;";

        var rows = (await connection.QueryAsync<VendorAgingRow>(sql, new
        {
            CompanyId = companyId,
            AsOfDate = asOfDateTime
        })).ToList();

        decimal grandTotal = rows.Sum(r => r.TotalOutstanding);

        return new VendorAgingReport
        {
            AsOfDate = asOf,
            GrandTotalOutstanding = grandTotal,
            Vendors = rows
        };
    }

    public async Task<VendorStatementReport> GetVendorStatementReportAsync(
        int vendorId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var from = (fromDate ?? new DateOnly(2020, 1, 1)).ToDateTime(TimeOnly.MinValue);
        var to = (toDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow.AddDays(1))).ToDateTime(TimeOnly.MaxValue);

        using var connection = _sqlConnectionFactory.CreateConnection();

        // 1. Get Vendor info
        const string vSql = "SELECT Id, VendorCode, VendorName FROM ap.Vendors WHERE Id = @VendorId AND CompanyId = @CompanyId;";
        var vendor = await connection.QueryFirstOrDefaultAsync<dynamic>(vSql, new { VendorId = vendorId, CompanyId = companyId });
        if (vendor == null)
        {
            throw new InvalidOperationException($"Vendor with ID {vendorId} was not found.");
        }

        // 2. Opening Balance
        const string opBillsSql = @"
            SELECT ISNULL(SUM(TotalAmount), 0) FROM ap.PurchaseBills 
            WHERE CompanyId = @CompanyId AND VendorId = @VendorId AND BillDate < @FromDate AND IsDeleted = 0 AND Status <> 0;";
        
        const string opPaymentsSql = @"
            SELECT ISNULL(SUM(Amount), 0) FROM ap.VendorPayments 
            WHERE CompanyId = @CompanyId AND VendorId = @VendorId AND PaymentDate < @FromDate AND IsDeleted = 0 AND Status <> 0;";

        decimal priorBills = await connection.ExecuteScalarAsync<decimal>(opBillsSql, new { CompanyId = companyId, VendorId = vendorId, FromDate = from });
        decimal priorPayments = await connection.ExecuteScalarAsync<decimal>(opPaymentsSql, new { CompanyId = companyId, VendorId = vendorId, FromDate = from });
        decimal openingBalance = priorBills - priorPayments;

        // 3. Chronological Transactions
        const string txSql = @"
            SELECT 
                b.BillDate AS TransactionDate,
                'BILL' AS DocumentType,
                b.BillNumber AS DocumentNumber,
                b.VendorInvoiceNumber AS Reference,
                b.TotalAmount AS BillAmount,
                0.0 AS PaidAmount
            FROM ap.PurchaseBills b
            WHERE b.CompanyId = @CompanyId AND b.VendorId = @VendorId AND b.BillDate >= @FromDate AND b.BillDate <= @ToDate AND b.IsDeleted = 0 AND b.Status <> 0
            
            UNION ALL
            
            SELECT 
                p.PaymentDate AS TransactionDate,
                'PAYMENT' AS DocumentType,
                p.PaymentNumber AS DocumentNumber,
                p.Reference AS Reference,
                0.0 AS BillAmount,
                p.Amount AS PaidAmount
            FROM ap.VendorPayments p
            WHERE p.CompanyId = @CompanyId AND p.VendorId = @VendorId AND p.PaymentDate >= @FromDate AND p.PaymentDate <= @ToDate AND p.IsDeleted = 0 AND p.Status <> 0
            
            ORDER BY TransactionDate ASC;";

        var txData = await connection.QueryAsync<dynamic>(txSql, new { CompanyId = companyId, VendorId = vendorId, FromDate = from, ToDate = to });

        decimal runningBal = openingBalance;
        decimal totalBilled = 0m;
        decimal totalPaid = 0m;
        var rows = new List<VendorStatementRow>();

        foreach (var r in txData)
        {
            decimal bAmt = (decimal)r.BillAmount;
            decimal pAmt = (decimal)r.PaidAmount;
            runningBal += (bAmt - pAmt);
            totalBilled += bAmt;
            totalPaid += pAmt;

            rows.Add(new VendorStatementRow
            {
                TransactionDate = (DateTime)r.TransactionDate,
                DocumentType = (string)r.DocumentType,
                DocumentNumber = (string)r.DocumentNumber,
                Reference = (string?)r.Reference,
                BillAmount = bAmt,
                PaidAmount = pAmt,
                RunningBalance = runningBal
            });
        }

        return new VendorStatementReport
        {
            VendorId = vendorId,
            VendorCode = (string)vendor.VendorCode,
            VendorName = (string)vendor.VendorName,
            OpeningBalance = openingBalance,
            TotalBilled = totalBilled,
            TotalPaid = totalPaid,
            ClosingBalance = runningBal,
            Rows = rows
        };
    }
}
