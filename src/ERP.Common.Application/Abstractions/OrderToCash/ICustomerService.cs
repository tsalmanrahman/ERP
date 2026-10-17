using ERP.Common.Application.Models.OrderToCash;

namespace ERP.Common.Application.Abstractions.OrderToCash;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByGuidAsync(Guid sysGuid, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroupDto>> GetCustomerGroupsAsync(CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteCustomerAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RestoreCustomerAsync(int id, CancellationToken cancellationToken = default);
}
