using Dapper;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Domain.Entities.Inventory;

namespace ERP.Common.Infrastructure.Services.Inventory;

public class InventoryReportingService : IInventoryReportingService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InventoryReportingService(
        ISqlConnectionFactory sqlConnectionFactory,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<StockLedgerReport> GetStockLedgerReportAsync(
        int productId,
        int? warehouseId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var from = fromDate ?? new DateTime(2020, 1, 1);
        var to = toDate ?? _dateTimeProvider.UtcNow.Date.AddDays(1);

        using var connection = _sqlConnectionFactory.CreateConnection();

        // 1. Get Product Details
        const string prodSql = @"
            SELECT p.Id, p.ProductCode, p.ProductName, u.UomCode
            FROM inv.Products p
            LEFT JOIN inv.UnitsOfMeasure u ON p.PrimaryUomId = u.Id
            WHERE p.Id = @ProductId AND p.CompanyId = @CompanyId;";

        var prodInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(prodSql, new { ProductId = productId, CompanyId = companyId });
        if (prodInfo == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} was not found.");
        }

        // 2. Calculate Opening Balance before `fromDate`
        const string opBalSql = @"
            SELECT ISNULL(SUM(QuantityIn - QuantityOut), 0)
            FROM inv.StockLedgers
            WHERE CompanyId = @CompanyId 
              AND ProductId = @ProductId
              AND (@WarehouseId IS NULL OR WarehouseId = @WarehouseId)
              AND TransactionDate < @FromDate
              AND IsDeleted = 0;";

        decimal openingBalance = await connection.ExecuteScalarAsync<decimal>(opBalSql, new
        {
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            FromDate = from
        });

        // 3. Retrieve Chronological Transactions
        const string txSql = @"
            SELECT 
                l.TransactionDate,
                l.TransactionType,
                l.ReferenceNumber,
                w.WarehouseName,
                p.ProductCode,
                p.ProductName,
                l.QuantityIn,
                l.QuantityOut,
                l.UnitCost,
                l.TotalCost,
                l.BalanceQuantity
            FROM inv.StockLedgers l
            INNER JOIN inv.Warehouses w ON l.WarehouseId = w.Id
            INNER JOIN inv.Products p ON l.ProductId = p.Id
            WHERE l.CompanyId = @CompanyId
              AND l.ProductId = @ProductId
              AND (@WarehouseId IS NULL OR l.WarehouseId = @WarehouseId)
              AND l.TransactionDate >= @FromDate
              AND l.TransactionDate <= @ToDate
              AND l.IsDeleted = 0
            ORDER BY l.TransactionDate ASC, l.Id ASC;";

        var rowsData = await connection.QueryAsync<dynamic>(txSql, new
        {
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            FromDate = from,
            ToDate = to
        });

        decimal runningQty = openingBalance;
        decimal totalIn = 0m;
        decimal totalOut = 0m;
        var rows = new List<StockLedgerReportRow>();

        foreach (var r in rowsData)
        {
            decimal qtyIn = (decimal)r.QuantityIn;
            decimal qtyOut = (decimal)r.QuantityOut;
            runningQty += (qtyIn - qtyOut);
            totalIn += qtyIn;
            totalOut += qtyOut;

            rows.Add(new StockLedgerReportRow
            {
                TransactionDate = (DateTime)r.TransactionDate,
                TransactionType = (StockTransactionType)(int)r.TransactionType,
                ReferenceNumber = (string)r.ReferenceNumber,
                WarehouseName = (string)r.WarehouseName,
                ProductCode = (string)r.ProductCode,
                ProductName = (string)r.ProductName,
                QuantityIn = qtyIn,
                QuantityOut = qtyOut,
                UnitCost = (decimal)r.UnitCost,
                TotalCost = (decimal)r.TotalCost,
                BalanceQuantity = runningQty
            });
        }

        return new StockLedgerReport
        {
            ProductId = productId,
            ProductCode = (string)prodInfo.ProductCode,
            ProductName = (string)prodInfo.ProductName,
            UomCode = prodInfo.UomCode ?? string.Empty,
            OpeningBalance = openingBalance,
            TotalIn = totalIn,
            TotalOut = totalOut,
            ClosingBalance = runningQty,
            Rows = rows
        };
    }

    public async Task<StockValuationReport> GetStockValuationSummaryAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                w.Id AS WarehouseId,
                w.WarehouseCode,
                w.WarehouseName,
                p.Id AS ProductId,
                p.ProductCode,
                p.ProductName,
                pc.ClassName,
                ISNULL(u.UomCode, '') AS UomCode,
                s.QuantityOnHand,
                s.AverageCost,
                (s.QuantityOnHand * s.AverageCost) AS TotalValuation
            FROM inv.ProductStock s
            INNER JOIN inv.Warehouses w ON s.WarehouseId = w.Id
            INNER JOIN inv.Products p ON s.ProductId = p.Id
            INNER JOIN inv.ProductClasses pc ON p.ProductClassId = pc.Id
            LEFT JOIN inv.UnitsOfMeasure u ON p.PrimaryUomId = u.Id
            WHERE s.CompanyId = @CompanyId
              AND (@WarehouseId IS NULL OR s.WarehouseId = @WarehouseId)
              AND s.IsDeleted = 0
              AND s.QuantityOnHand <> 0
            ORDER BY w.WarehouseCode, p.ProductCode;";

        var items = (await connection.QueryAsync<StockValuationSummaryRow>(sql, new
        {
            CompanyId = companyId,
            WarehouseId = warehouseId
        })).ToList();

        decimal totalValuation = items.Sum(i => i.TotalValuation);

        return new StockValuationReport
        {
            AsOfDate = _dateTimeProvider.UtcNow.Date,
            TotalInventoryValuation = totalValuation,
            TotalItemCount = items.Count,
            Items = items
        };
    }
}
