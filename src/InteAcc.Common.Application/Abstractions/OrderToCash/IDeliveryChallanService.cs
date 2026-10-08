using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Common.Application.Abstractions.OrderToCash;

public interface IDeliveryChallanService
{
    Task<IReadOnlyList<DeliveryChallanDto>> GetDeliveryChallansAsync(int? salesOrderId = null, CancellationToken cancellationToken = default);
    Task<DeliveryChallanDto?> GetDeliveryChallanByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DeliveryChallanDto> CreateDeliveryChallanAsync(CreateDeliveryChallanRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryChallanDto> DispatchAndPostCogsAsync(int id, CancellationToken cancellationToken = default);
}
