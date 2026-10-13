using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Migration;
using InteAcc.Common.Application.Models.Migration;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;

    public MigrationController(IDataMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    [HttpPost("run-etl")]
    public async Task<ActionResult<EtlMigrationSummaryDto>> RunEtl(
        [FromBody] EtlMigrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _migrationService.ExecuteEtlMigrationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<ModuleMigrationSummaryDto>>> GetHistory(
        [FromQuery] int companyId = 1,
        CancellationToken cancellationToken = default)
    {
        var history = await _migrationService.GetMigrationHistoryAsync(companyId, cancellationToken);
        return Ok(history);
    }

    [HttpGet("archives")]
    public async Task<ActionResult<List<HistoricalArchiveDto>>> GetArchives(
        [FromQuery] int companyId = 1,
        [FromQuery] string? fiscalYear = null,
        [FromQuery] string? accountCode = null,
        CancellationToken cancellationToken = default)
    {
        var archives = await _migrationService.GetHistoricalArchivesAsync(companyId, fiscalYear, accountCode, cancellationToken);
        return Ok(archives);
    }
}
