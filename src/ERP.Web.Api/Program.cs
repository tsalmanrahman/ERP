using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ERP.Common.Application;
using ERP.Common.Application.Security;
using ERP.Common.Infrastructure;
using ERP.Web.Api.Health;
using ERP.Common.Infrastructure.Services.Identity;

const string CorsPolicyName = "ErpClients";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "ERP API";
        return Task.CompletedTask;
    });
});

// Cross-Origin Resource Sharing (CORS).
// The bundled web portal is served from the same origin as the API and needs no CORS.
// Development allows any origin; every other environment is limited to the explicit
// "Cors:AllowedOrigins" list (empty list = same-origin only).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// Clean Architecture Layers
builder.Services.AddCommonApplication();
builder.Services.AddCommonInfrastructure(builder.Configuration);

// JWT Authentication (settings are shared with JwtTokenService; startup fails fast if the signing key is missing)
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()?.EnsureValid()
    ?? throw new InvalidOperationException($"Configuration section '{JwtSettings.SectionName}' was not found.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenRevocationService = context.HttpContext.RequestServices.GetRequiredService<ERP.Common.Application.Abstractions.Identity.ITokenRevocationService>();
                var authHeader = context.Request.Headers["Authorization"].ToString();
                var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authHeader.Substring(7).Trim() : authHeader.Trim();
                if (!string.IsNullOrEmpty(token) && tokenRevocationService.IsTokenRevoked(token))
                {
                    context.Fail("Token has been revoked.");
                }
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization();

// Enterprise Health Check Probes
builder.Services.AddHealthChecks()
    .AddCheck<SqlDatabaseHealthCheck>("sqlserver_db", tags: new[] { "ready" });

var app = builder.Build();

// Reverse Proxy & Forwarded Headers Configuration (Nginx / YARP)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Unhandled exceptions become RFC 7807 problem responses without leaking stack traces outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(CorsPolicyName);

// In containerized deployments behind reverse proxy, HTTPS termination is handled by proxy.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness probe succeeds if web host process is responsive
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") // Readiness probe validates database connectivity
});

app.MapControllers();

app.Run();
