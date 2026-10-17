using ERP.Common.Application.Models.ProcureToPay;

namespace ERP.Common.Application.Abstractions.ProcureToPay;

public interface IVendorPaymentService
{
    Task<List<VendorPaymentDto>> GetVendorPaymentsAsync(int? vendorId = null, CancellationToken cancellationToken = default);
    Task<VendorPaymentDto?> GetVendorPaymentByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes vendor settlement via Bank Transfer, Cheque, or Cash.
    /// Deducts vendor outstanding balance, allocates against target bills,
    /// and generates a balanced double-entry GL journal:
    /// Debit Accounts Payable (2110), Credit Bank (1120) or Cash (1111).
    /// </summary>
    Task<VendorPaymentDto> ProcessVendorPaymentAsync(CreateVendorPaymentRequest request, CancellationToken cancellationToken = default);
}
