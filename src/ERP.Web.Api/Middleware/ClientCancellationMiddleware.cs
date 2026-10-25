namespace ERP.Web.Api.Middleware;

/// <summary>
/// Catches OperationCanceledException thrown when a client aborts an HTTP request
/// (e.g., page refresh, tab close, or browser navigation) and gracefully returns 
/// HTTP 499 (Client Closed Request) without bubbling up as an unhandled server error.
/// </summary>
public class ClientCancellationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClientCancellationMiddleware> _logger;

    public ClientCancellationMiddleware(RequestDelegate next, ILogger<ClientCancellationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("HTTP request to {Path} was canceled by the client.", context.Request.Path);
            if (!context.Response.HasStarted)
            {
                // Standard HTTP status code 499 for client closed connection
                context.Response.StatusCode = 499;
            }
        }
    }
}
