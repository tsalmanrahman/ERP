using ERP.Common.Application.Models.Specialized;

namespace ERP.Common.Application.Abstractions.Specialized;

public interface IFixedAssetService
{
    Task<IReadOnlyList<AssetGroupDto>> GetAssetGroupsAsync(CancellationToken cancellationToken = default);
    Task<AssetGroupDto> CreateAssetGroupAsync(CreateAssetGroupRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(int? assetGroupId = null, bool? isDisposed = null, CancellationToken cancellationToken = default);
    Task<AssetDto?> GetAssetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);

    Task<DepreciationRunResultDto> RunDepreciationAsync(RunDepreciationRequest request, CancellationToken cancellationToken = default);
    Task<AssetDisposalResultDto> DisposeAssetAsync(DisposeAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetScheduleReportDto> GetAssetScheduleReportAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);
}
