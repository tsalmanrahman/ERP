using ERP.Common.Application.Models.ProcureToPay;

namespace ERP.Common.Application.Abstractions.ProcureToPay;

public interface IVendorService
{
    Task<List<VendorDto>> GetVendorsAsync(CancellationToken cancellationToken = default);
    Task<VendorDto?> GetVendorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VendorDto?> GetVendorByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteVendorAsync(int id, CancellationToken cancellationToken = default);
    Task<List<VendorGroupDto>> GetVendorGroupsAsync(CancellationToken cancellationToken = default);
}
