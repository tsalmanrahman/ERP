using ERP.Common.Application.Abstractions.Common;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Api.Middleware;

/// <summary>
/// Enforces tenant company isolation on incoming HTTP requests.
/// Rejects authenticated requests specifying an unauthorized or malformed X-Company-Id header with HTTP 403/400,
/// and translates domain UnauthorizedAccessException into HTTP 403 Forbidden ProblemDetails.
/// </summary>
public class CompanyContextEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CompanyContextEnforcementMiddleware> _logger;

    public CompanyContextEnforcementMiddleware(
        RequestDelegate next,
        ILogger<CompanyContextEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserContext currentUserContext)
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Path.StartsWithSegments("/api/v1/auth/login") &&
            context.Request.Headers.TryGetValue("X-Company-Id", out var headerValues))
        {
            var rawHeader = headerValues.ToString().Trim();
            if (!string.IsNullOrEmpty(rawHeader))
            {
                if (!int.TryParse(rawHeader, out var requestedCompanyId) || requestedCompanyId <= 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Invalid Company Context",
                        Detail = $"The X-Company-Id header value '{rawHeader}' is not a valid company identifier."
                    });
                    return;
                }

                if (currentUserContext.IsAuthenticated && !currentUserContext.HasAccessToCompany(requestedCompanyId))
                {
                    _logger.LogWarning(
                        "Unauthorized cross-company access attempt blocked: User '{UserName}' (ID: {UserId}) requested CompanyId={CompanyId} on {Method} {Path}.",
                        currentUserContext.UserName,
                        currentUserContext.UserId,
                        requestedCompanyId,
                        context.Request.Method,
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Company Access Denied",
                        Detail = $"You are not authorized to access or modify records for Company ID {requestedCompanyId}."
                    });
                    return;
                }
            }
        }

        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Company authorization check failed on {Method} {Path}", context.Request.Method, context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Company Isolation Violation",
                    Detail = ex.Message
                });
            }
        }
    }
}
