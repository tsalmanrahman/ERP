using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Identity;
using InteAcc.Common.Application.Models.Auth;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/sa/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService _authService)
    {
        this._authService = _authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return Unauthorized(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        int? activeCompanyId = null;
        if (int.TryParse(User.FindFirst("company_id")?.Value, out var compId))
        {
            activeCompanyId = compId;
        }

        var result = await _authService.GetCurrentUserProfileAsync(userId, activeCompanyId, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost("switch-company")]
    [Authorize]
    public async Task<IActionResult> SwitchCompany([FromBody] SwitchCompanyRequest request, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.SwitchCompanyAsync(userId, request.TargetCompanyId, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }
}
