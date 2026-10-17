using ERP.Common.Application.Models.Inventory;

namespace ERP.Common.Application.Abstractions.Inventory;

public interface IStockMovementService
{
    // Stock Adjustments (Cycle Count / Physical Variance / Scrap)
    Task<List<StockAdjustmentDto>> GetStockAdjustmentsAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto?> GetStockAdjustmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> ProcessStockAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default);

    // Stock Transfers between Warehouses
    Task<List<StockTransferDto>> GetStockTransfersAsync(CancellationToken cancellationToken = default);
    Task<StockTransferDto?> GetStockTransferByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ProcessStockTransferAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
}
