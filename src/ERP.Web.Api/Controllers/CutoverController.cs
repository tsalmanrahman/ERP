using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Migration;
using ERP.Common.Application.Models.Migration;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CutoverController : ControllerBase
{
    private readonly ICutoverService _cutoverService;
    private readonly ICurrentUserContext _currentUserContext;

    public CutoverController(ICutoverService cutoverService, ICurrentUserContext currentUserContext)
    {
        _cutoverService = cutoverService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet("checklist")]
    public async Task<ActionResult<CutoverChecklistResponse>> GetChecklist(
        [FromQuery] int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var targetCompanyId = companyId ?? _currentUserContext.GetRequiredCompanyId();
        if (!_currentUserContext.HasAccessToCompany(targetCompanyId))
        {
            return Forbid();
        }

        var response = await _cutoverService.EvaluateCutoverChecklistAsync(targetCompanyId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("signoff")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<CutoverSignoffResult>> Signoff(
        [FromBody] CutoverSignoffRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.HasAccessToCompany(request.CompanyId))
        {
            return Forbid();
        }

        var result = await _cutoverService.ExecuteCutoverSignoffAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
