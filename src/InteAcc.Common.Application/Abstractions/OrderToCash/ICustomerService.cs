using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Common.Application.Abstractions.OrderToCash;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroupDto>> GetCustomerGroupsAsync(CancellationToken cancellationToken = default);
}
