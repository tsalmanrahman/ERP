using Microsoft.Extensions.Diagnostics.HealthChecks;
using ERP.Common.Application.Abstractions.Data;

namespace ERP.Web.Api.Health;

public class SqlDatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseHealthService _databaseHealthService;

    public SqlDatabaseHealthCheck(IDatabaseHealthService databaseHealthService)
    {
        _databaseHealthService = databaseHealthService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await _databaseHealthService.CheckDatabaseHealthAsync(cancellationToken);
        if (result.IsSuccess)
        {
            var data = new Dictionary<string, object>
            {
                { "DatabaseName", result.Value.DatabaseName },
                { "ServerVersion", result.Value.ServerVersion },
                { "ServerUtcTime", result.Value.ServerUtcTime },
                { "Collation", result.Value.Collation }
            };
            return HealthCheckResult.Healthy("SQL Server database connection operational and healthy.", data);
        }

        return HealthCheckResult.Unhealthy($"SQL Server database check failed: {result.Error}");
    }
}
