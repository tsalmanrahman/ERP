using ERP.Common.Application.Models.Inventory;

namespace ERP.Common.Application.Abstractions.Inventory;

public interface IGoodsReceiptService
{
    Task<List<GrnDto>> GetGoodsReceiptNotesAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<GrnDto?> GetGoodsReceiptNoteByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GrnDto> CreateGoodsReceiptNoteAsync(CreateGrnRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Receives goods into warehouse stock, updates weighted average cost,
    /// updates linked Purchase Order / Production Order balances,
    /// appends stock ledger records, and automatically posts a balanced double-entry
    /// General Ledger voucher (Dr 1140 Inventory / Cr 2140 GRN Clearing or 1145 WIP Clearing).
    /// </summary>
    Task<GrnDto> ReceiveAndPostGrnAsync(int grnId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches eligible Purchase Orders with outstanding (unreceived) quantities for Vendor GRN intake.
    /// </summary>
    Task<List<EligiblePurchaseOrderDto>> GetEligiblePurchaseOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches eligible Production Orders with outstanding (uncompleted) quantities for Production GRN intake.
    /// </summary>
    Task<List<EligibleProductionOrderDto>> GetEligibleProductionOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a draft Goods Receipt Return referencing an original GRN.
    /// </summary>
    Task<GrnReturnDto> CreateGoodsReceiptReturnAsync(CreateGrnReturnRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a Goods Receipt Return: decrements inventory, restores PO/MO balances, and posts GL reversal voucher.
    /// </summary>
    Task<GrnReturnDto> PostGoodsReceiptReturnAsync(int returnId, CancellationToken cancellationToken = default);
}
