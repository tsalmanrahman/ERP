using ERP.Common.Application.Models.ProcureToPay;

namespace ERP.Common.Application.Abstractions.ProcureToPay;

public interface IPurchaseBillService
{
    Task<List<PurchaseBillDto>> GetPurchaseBillsAsync(int? vendorId = null, CancellationToken cancellationToken = default);
    Task<PurchaseBillDto?> GetPurchaseBillByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseBillDto> CreatePurchaseBillAsync(CreatePurchaseBillRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes 3-Way Matching comparison between Purchase Order, Goods Receipt Note, and Vendor Bill.
    /// Flags discrepancies in billed quantities vs received quantities, or billed unit price vs PO unit price.
    /// </summary>
    Task<ThreeWayMatchResultDto> PerformThreeWayMatchAsync(int billId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms and posts the vendor bill to the General Ledger:
    /// Debits GRN Clearing (2140), debits tax/VAT (2130), and credits Trade Accounts Payable (2110).
    /// Updates vendor outstanding balance.
    /// </summary>
    Task<PurchaseBillDto> ConfirmAndPostBillAsync(int billId, CancellationToken cancellationToken = default);
}
