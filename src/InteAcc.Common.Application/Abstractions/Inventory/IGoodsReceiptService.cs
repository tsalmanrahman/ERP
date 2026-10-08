using InteAcc.Common.Application.Models.Inventory;

namespace InteAcc.Common.Application.Abstractions.Inventory;

public interface IGoodsReceiptService
{
    Task<List<GrnDto>> GetGoodsReceiptNotesAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<GrnDto?> GetGoodsReceiptNoteByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GrnDto> CreateGoodsReceiptNoteAsync(CreateGrnRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Receives goods into warehouse stock, updates weighted average cost,
    /// appends stock ledger records, and automatically posts a balanced double-entry
    /// General Ledger voucher (re-engineering legacy icProcessTransactionToGl).
    /// </summary>
    Task<GrnDto> ReceiveAndPostGrnAsync(int grnId, CancellationToken cancellationToken = default);
}
