using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ERP.Common.Application.Abstractions.Common;

namespace ERP.Common.Infrastructure.Services.Common;

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

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool HasAccessToCompany(int companyId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !IsAuthenticated) return false;

        // SuperAdmin has access across all tenant companies
        if (user.IsInRole("SUPERADMIN") || user.IsInRole("SuperAdmin"))
        {
            return true;
        }

        // Check if matching current token active company
        var claimValue = user.FindFirst("company_id")?.Value;
        if (int.TryParse(claimValue, out var activeCompanyId) && activeCompanyId == companyId)
        {
            return true;
        }

        // Check accessible companies claims
        return user.FindAll("accessible_company")
            .Any(c => int.TryParse(c.Value, out var id) && id == companyId);
    }

    public int? CompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var user = httpContext.User;
            var claimValue = user?.FindFirst("company_id")?.Value;
            int.TryParse(claimValue, out var companyIdFromClaim);

            // 1. Explicit tenant header X-Company-Id
            if (httpContext.Request.Headers.TryGetValue("X-Company-Id", out var headerValue) &&
                int.TryParse(headerValue, out var companyIdFromHeader))
            {
                // Verify caller has legitimate rights to this tenant
                if (!IsAuthenticated || HasAccessToCompany(companyIdFromHeader))
                {
                    return companyIdFromHeader;
                }

                // Do not silently fall back to another company when an explicit unauthorized X-Company-Id header is sent
                return null;
            }

            // 2. JWT claim 'company_id'
            if (companyIdFromClaim > 0)
            {
                return companyIdFromClaim;
            }

            return null;
        }
    }

    public int GetRequiredCompanyId()
    {
        var companyId = CompanyId;
        if (companyId is > 0)
        {
            return companyId.Value;
        }

        throw new InvalidOperationException("An authorized active Company context is required for this operation.");
    }
}
