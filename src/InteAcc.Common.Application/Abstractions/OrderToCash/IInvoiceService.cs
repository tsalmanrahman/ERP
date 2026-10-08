using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Common.Application.Abstractions.OrderToCash;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(int? customerId = null, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> ConfirmAndPostToGlAsync(int id, CancellationToken cancellationToken = default);
}
