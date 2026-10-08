using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Common.Application.Abstractions.OrderToCash;

public interface ISalesOrderService
{
    Task<IReadOnlyList<SalesOrderDto>> GetSalesOrdersAsync(int? customerId = null, CancellationToken cancellationToken = default);
    Task<SalesOrderDto?> GetSalesOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> ConfirmSalesOrderAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesCenterDto>> GetSalesCentersAsync(CancellationToken cancellationToken = default);
    Task<SalesCenterDto> CreateSalesCenterAsync(CreateSalesCenterRequest request, CancellationToken cancellationToken = default);
}
