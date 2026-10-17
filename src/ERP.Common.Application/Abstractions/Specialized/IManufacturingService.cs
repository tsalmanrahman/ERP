using ERP.Common.Application.Models.Specialized;

namespace ERP.Common.Application.Abstractions.Specialized;

public interface IManufacturingService
{
    Task<IReadOnlyList<BillOfMaterialsDto>> GetBomsAsync(int? finishedGoodProductId = null, CancellationToken cancellationToken = default);
    Task<BillOfMaterialsDto?> GetBomByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BillOfMaterialsDto> CreateBomAsync(CreateBomRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionOrderDto>> GetProductionOrdersAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<ProductionOrderDto?> GetProductionOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionOrderDto> CreateProductionOrderAsync(CreateProductionOrderRequest request, CancellationToken cancellationToken = default);
    Task<ProductionOrderDto> CompleteProductionOrderAsync(CompleteProductionOrderRequest request, CancellationToken cancellationToken = default);
}
