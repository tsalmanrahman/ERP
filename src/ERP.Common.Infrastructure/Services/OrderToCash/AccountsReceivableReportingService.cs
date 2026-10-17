using Dapper;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.OrderToCash;

namespace ERP.Common.Infrastructure.Services.OrderToCash;

public class AccountsReceivableReportingService : IAccountsReceivableReportingService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AccountsReceivableReportingService(
        ISqlConnectionFactory sqlConnectionFactory,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<CustomerAgingReport> GetCustomerAgingReportAsync(
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var asOf = asOfDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var asOfDateTime = asOf.ToDateTime(TimeOnly.MinValue);

        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                c.Id AS CustomerId,
                c.CustomerCode,
                c.CustomerName,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, i.DueDate, @AsOfDate) <= 30 THEN i.BalanceAmount ELSE 0 END), 0) AS CurrentDays,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, i.DueDate, @AsOfDate) BETWEEN 31 AND 60 THEN i.BalanceAmount ELSE 0 END), 0) AS ThirtyToSixty,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, i.DueDate, @AsOfDate) BETWEEN 61 AND 90 THEN i.BalanceAmount ELSE 0 END), 0) AS SixtyToNinety,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, i.DueDate, @AsOfDate) BETWEEN 91 AND 120 THEN i.BalanceAmount ELSE 0 END), 0) AS NinetyToOneTwenty,
                ISNULL(SUM(CASE WHEN DATEDIFF(day, i.DueDate, @AsOfDate) > 120 THEN i.BalanceAmount ELSE 0 END), 0) AS OverOneTwenty,
                ISNULL(SUM(i.BalanceAmount), 0) AS TotalOutstanding
            FROM ar.Customers c
            LEFT JOIN ar.Invoices i ON c.Id = i.CustomerId AND i.CompanyId = @CompanyId AND i.BalanceAmount > 0 AND i.IsDeleted = 0
            WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0
            GROUP BY c.Id, c.CustomerCode, c.CustomerName, c.CurrentBalance
            HAVING ISNULL(SUM(i.BalanceAmount), 0) > 0 OR c.CurrentBalance > 0
            ORDER BY TotalOutstanding DESC;";

        var rows = (await connection.QueryAsync<CustomerAgingRow>(sql, new
        {
            CompanyId = companyId,
            AsOfDate = asOfDateTime
        })).ToList();

        decimal grandTotal = rows.Sum(r => r.TotalOutstanding);

        return new CustomerAgingReport
        {
            AsOfDate = asOf,
            GrandTotalOutstanding = grandTotal,
            Customers = rows
        };
    }

    public async Task<CustomerStatementReport> GetCustomerStatementReportAsync(
        int customerId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var from = (fromDate ?? new DateOnly(2020, 1, 1)).ToDateTime(TimeOnly.MinValue);
        var to = (toDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow.AddDays(1))).ToDateTime(TimeOnly.MaxValue);

        using var connection = _sqlConnectionFactory.CreateConnection();

        // 1. Get Customer info
        const string cSql = "SELECT Id, CustomerCode, CustomerName FROM ar.Customers WHERE Id = @CustomerId AND CompanyId = @CompanyId;";
        var customer = await connection.QueryFirstOrDefaultAsync<dynamic>(cSql, new { CustomerId = customerId, CompanyId = companyId });
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {customerId} was not found.");
        }

        // 2. Opening Balance
        const string opInvoicesSql = @"
            SELECT ISNULL(SUM(TotalAmount), 0) FROM ar.Invoices 
            WHERE CompanyId = @CompanyId AND CustomerId = @CustomerId AND InvoiceDate < @FromDate AND IsDeleted = 0 AND Status <> 0;";
        
        const string opReceiptsSql = @"
            SELECT ISNULL(SUM(Amount), 0) FROM ar.CustomerReceipts 
            WHERE CompanyId = @CompanyId AND CustomerId = @CustomerId AND ReceiptDate < @FromDate AND IsDeleted = 0 AND Status <> 0;";

        decimal priorInvoices = await connection.ExecuteScalarAsync<decimal>(opInvoicesSql, new { CompanyId = companyId, CustomerId = customerId, FromDate = from });
        decimal priorReceipts = await connection.ExecuteScalarAsync<decimal>(opReceiptsSql, new { CompanyId = companyId, CustomerId = customerId, FromDate = from });
        decimal openingBalance = priorInvoices - priorReceipts;

        // 3. Chronological Transactions
        const string txSql = @"
            SELECT 
                i.InvoiceDate AS TransactionDate,
                'INVOICE' AS DocumentType,
                i.InvoiceNumber AS DocumentNumber,
                NULL AS Reference,
                i.TotalAmount AS InvoiceAmount,
                0.0 AS ReceiptAmount
            FROM ar.Invoices i
            WHERE i.CompanyId = @CompanyId AND i.CustomerId = @CustomerId AND i.InvoiceDate >= @FromDate AND i.InvoiceDate <= @ToDate AND i.IsDeleted = 0 AND i.Status <> 0
            
            UNION ALL
            
            SELECT 
                r.ReceiptDate AS TransactionDate,
                'RECEIPT' AS DocumentType,
                r.ReceiptNumber AS DocumentNumber,
                r.Reference AS Reference,
                0.0 AS InvoiceAmount,
                r.Amount AS ReceiptAmount
            FROM ar.CustomerReceipts r
            WHERE r.CompanyId = @CompanyId AND r.CustomerId = @CustomerId AND r.ReceiptDate >= @FromDate AND r.ReceiptDate <= @ToDate AND r.IsDeleted = 0 AND r.Status <> 0
            
            ORDER BY TransactionDate ASC;";

        var txData = await connection.QueryAsync<dynamic>(txSql, new { CompanyId = companyId, CustomerId = customerId, FromDate = from, ToDate = to });

        decimal runningBal = openingBalance;
        decimal totalInvoiced = 0m;
        decimal totalReceipt = 0m;
        var rows = new List<CustomerStatementRow>();

        foreach (var r in txData)
        {
            decimal invAmt = (decimal)r.InvoiceAmount;
            decimal rcpAmt = (decimal)r.ReceiptAmount;
            runningBal += (invAmt - rcpAmt);
            totalInvoiced += invAmt;
            totalReceipt += rcpAmt;

            rows.Add(new CustomerStatementRow
            {
                TransactionDate = (DateTime)r.TransactionDate,
                DocumentType = (string)r.DocumentType,
                DocumentNumber = (string)r.DocumentNumber,
                Reference = (string?)r.Reference,
                InvoiceAmount = invAmt,
                ReceiptAmount = rcpAmt,
                RunningBalance = runningBal
            });
        }

        return new CustomerStatementReport
        {
            CustomerId = customerId,
            CustomerCode = (string)customer.CustomerCode,
            CustomerName = (string)customer.CustomerName,
            OpeningBalance = openingBalance,
            TotalInvoiced = totalInvoiced,
            TotalReceipt = totalReceipt,
            ClosingBalance = runningBal,
            Rows = rows
        };
    }
}
