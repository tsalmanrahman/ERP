using InteAcc.Common.Application.Models.ProcureToPay;

namespace InteAcc.Common.Application.Abstractions.ProcureToPay;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(int? vendorId = null, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> IssuePurchaseOrderAsync(int id, CancellationToken cancellationToken = default);
}
