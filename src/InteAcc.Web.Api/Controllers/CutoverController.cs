using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Migration;
using InteAcc.Common.Application.Models.Migration;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CutoverController : ControllerBase
{
    private readonly ICutoverService _cutoverService;

    public CutoverController(ICutoverService cutoverService)
    {
        _cutoverService = cutoverService;
    }

    [HttpGet("checklist")]
    public async Task<ActionResult<CutoverChecklistResponse>> GetChecklist(
        [FromQuery] int companyId = 1,
        CancellationToken cancellationToken = default)
    {
        var response = await _cutoverService.EvaluateCutoverChecklistAsync(companyId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("signoff")]
    public async Task<ActionResult<CutoverSignoffResult>> Signoff(
        [FromBody] CutoverSignoffRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _cutoverService.ExecuteCutoverSignoffAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
