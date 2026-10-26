using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Migration;
using ERP.Common.Application.Models.Migration;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MigrationController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;
    private readonly ICurrentUserContext _currentUserContext;

    public MigrationController(IDataMigrationService migrationService, ICurrentUserContext currentUserContext)
    {
        _migrationService = migrationService;
        _currentUserContext = currentUserContext;
    }

    [HttpPost("run-etl")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<EtlMigrationSummaryDto>> RunEtl(
        [FromBody] EtlMigrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserContext.HasAccessToCompany(request.CompanyId))
        {
            return Forbid();
        }

        var result = await _migrationService.ExecuteEtlMigrationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<ModuleMigrationSummaryDto>>> GetHistory(
        [FromQuery] int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var targetCompanyId = companyId ?? _currentUserContext.GetRequiredCompanyId();
        if (!_currentUserContext.HasAccessToCompany(targetCompanyId))
        {
            return Forbid();
        }

        var history = await _migrationService.GetMigrationHistoryAsync(targetCompanyId, cancellationToken);
        return Ok(history);
    }

    [HttpGet("archives")]
    public async Task<ActionResult<List<HistoricalArchiveDto>>> GetArchives(
        [FromQuery] int? companyId = null,
        [FromQuery] string? fiscalYear = null,
        [FromQuery] string? accountCode = null,
        CancellationToken cancellationToken = default)
    {
        var targetCompanyId = companyId ?? _currentUserContext.GetRequiredCompanyId();
        if (!_currentUserContext.HasAccessToCompany(targetCompanyId))
        {
            return Forbid();
        }

        var archives = await _migrationService.GetHistoricalArchivesAsync(targetCompanyId, fiscalYear, accountCode, cancellationToken);
        return Ok(archives);
    }
}
