using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.SystemAdmin;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/sa/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _companyService.GetAccessibleCompaniesAsync(userId, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }
}
