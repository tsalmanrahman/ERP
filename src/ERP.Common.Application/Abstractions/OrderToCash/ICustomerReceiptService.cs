using ERP.Common.Application.Models.OrderToCash;

namespace ERP.Common.Application.Abstractions.OrderToCash;

public interface ICustomerReceiptService
{
    Task<IReadOnlyList<CustomerReceiptDto>> GetCustomerReceiptsAsync(int? customerId = null, CancellationToken cancellationToken = default);
    Task<CustomerReceiptDto?> GetCustomerReceiptByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerReceiptDto> ProcessCustomerReceiptAsync(CreateCustomerReceiptRequest request, CancellationToken cancellationToken = default);
}
