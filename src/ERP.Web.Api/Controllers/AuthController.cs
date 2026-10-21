using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Identity;
using ERP.Common.Application.Models.Auth;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/sa/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenRevocationService _tokenRevocationService;

    public AuthController(IAuthService _authService, ITokenRevocationService _tokenRevocationService)
    {
        this._authService = _authService;
        this._tokenRevocationService = _tokenRevocationService;
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

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring(7).Trim()
            : authHeader.Trim();

        if (!string.IsNullOrEmpty(token))
        {
            _tokenRevocationService.RevokeToken(token);
        }

        return Ok(new { success = true, message = "Successfully logged out." });
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

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.UpdateProfileAsync(userId, request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, message = "Password updated successfully." });
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
