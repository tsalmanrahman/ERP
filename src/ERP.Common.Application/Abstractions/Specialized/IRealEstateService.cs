using ERP.Common.Application.Models.Specialized;

namespace ERP.Common.Application.Abstractions.Specialized;

public interface IRealEstateService
{
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyUnitDto>> GetPropertyUnitsAsync(int? projectId = null, string? status = null, CancellationToken cancellationToken = default);
    Task<PropertyUnitDto> CreatePropertyUnitAsync(CreatePropertyUnitRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyBookingDto>> GetPropertyBookingsAsync(int? projectId = null, int? customerId = null, CancellationToken cancellationToken = default);
    Task<PropertyBookingDto?> GetPropertyBookingByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PropertyBookingDto> CreatePropertyBookingAsync(CreatePropertyBookingRequest request, CancellationToken cancellationToken = default);
    Task<InstallmentCollectionResultDto> CollectInstallmentAsync(CollectInstallmentRequest request, CancellationToken cancellationToken = default);
}
