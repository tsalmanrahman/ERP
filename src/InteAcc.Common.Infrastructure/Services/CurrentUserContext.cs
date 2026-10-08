using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using InteAcc.Common.Application.Abstractions.Common;

namespace InteAcc.Common.Infrastructure.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("unique_name")?.Value;

    public int? CompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // 1. First precedence: Explicit tenant header X-Company-Id
            if (httpContext.Request.Headers.TryGetValue("X-Company-Id", out var headerValue) &&
                int.TryParse(headerValue, out var companyIdFromHeader))
            {
                return companyIdFromHeader;
            }

            // 2. Second precedence: JWT claim 'company_id'
            var claimValue = httpContext.User?.FindFirst("company_id")?.Value;
            if (int.TryParse(claimValue, out var companyIdFromClaim))
            {
                return companyIdFromClaim;
            }

            return null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
