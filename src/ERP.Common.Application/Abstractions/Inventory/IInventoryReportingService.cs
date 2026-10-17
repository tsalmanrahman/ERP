using ERP.Common.Application.Models.Inventory;

namespace ERP.Common.Application.Abstractions.Inventory;

public interface IInventoryReportingService
{
    Task<StockLedgerReport> GetStockLedgerReportAsync(int productId, int? warehouseId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<StockValuationReport> GetStockValuationSummaryAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
}
