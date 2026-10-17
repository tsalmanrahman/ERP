using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Common;

namespace ERP.Web.Api.Controllers;

public class SetSequenceConfigRequest
{
    public string EntityType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int NumberLength { get; set; } = 6;
    public string FiscalYear { get; set; } = "GLOBAL";
    public bool IsActive { get; set; } = true;
}

[ApiController]
[Route("api/sa/[controller]")]
[Authorize]
public class SequencesController : ControllerBase
{
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ICurrentUserContext _userContext;

    public SequencesController(IDocumentSequenceService sequenceService, ICurrentUserContext userContext)
    {
        _sequenceService = sequenceService;
        _userContext = userContext;
    }

    private int GetCompanyId() => _userContext.CompanyId ?? 1;

    [HttpGet]
    public async Task<IActionResult> GetAllConfigurations(CancellationToken cancellationToken)
    {
        var configs = await _sequenceService.GetAllConfigurationsAsync(GetCompanyId(), cancellationToken);
        return Ok(new { success = true, data = configs });
    }

    [HttpGet("{entityType}")]
    public async Task<IActionResult> GetConfiguration(string entityType, CancellationToken cancellationToken)
    {
        var config = await _sequenceService.GetConfigurationAsync(GetCompanyId(), entityType, cancellationToken);
        return Ok(new { success = true, data = config });
    }

    [HttpPost]
    public async Task<IActionResult> SetConfiguration([FromBody] SetSequenceConfigRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityType) || string.IsNullOrWhiteSpace(request.Prefix))
        {
            return BadRequest(new { success = false, message = "Entity type and prefix are required." });
        }

        try
        {
            await _sequenceService.SetConfigurationAsync(
                GetCompanyId(),
                request.EntityType,
                request.Prefix,
                request.NumberLength,
                request.FiscalYear,
                request.IsActive,
                cancellationToken);

            var updated = await _sequenceService.GetConfigurationAsync(GetCompanyId(), request.EntityType, cancellationToken);
            return Ok(new { success = true, data = updated, message = $"Prefix sequence for '{request.EntityType}' configured successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
