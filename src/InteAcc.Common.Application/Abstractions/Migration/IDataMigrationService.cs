using InteAcc.Common.Application.Models.Migration;

namespace InteAcc.Common.Application.Abstractions.Migration;

public interface IDataMigrationService
{
    Task<EtlMigrationSummaryDto> ExecuteEtlMigrationAsync(EtlMigrationRequest request, CancellationToken cancellationToken = default);
    Task<List<ModuleMigrationSummaryDto>> GetMigrationHistoryAsync(int companyId, CancellationToken cancellationToken = default);
    Task<List<HistoricalArchiveDto>> GetHistoricalArchivesAsync(int companyId, string? fiscalYear, string? accountCode, CancellationToken cancellationToken = default);
}
