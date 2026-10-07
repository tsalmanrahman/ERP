using Dapper;
using InteAcc.Common.Application.Abstractions.Data;
using InteAcc.Common.Application.Models;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Infrastructure.Services;

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public DatabaseHealthService(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<Result<DatabaseHealthDto>> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _sqlConnectionFactory.CreateConnection();
            
            const string sql = @"
                SELECT 
                    DB_NAME() AS DatabaseName,
                    @@VERSION AS ServerVersion,
                    GETUTCDATE() AS ServerUtcTime,
                    CAST(compatibility_level AS VARCHAR(10)) AS CompatibilityLevel,
                    collation_name AS Collation
                FROM sys.databases
                WHERE name = DB_NAME();";

            var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
            var info = await connection.QueryFirstOrDefaultAsync<dynamic>(command);

            if (info == null)
            {
                return Result.Failure<DatabaseHealthDto>("Failed to retrieve database metadata.");
            }

            var dto = new DatabaseHealthDto(
                IsConnected: true,
                DatabaseName: (string)info.DatabaseName,
                ServerVersion: (string)info.ServerVersion,
                ServerUtcTime: (DateTime)info.ServerUtcTime,
                CompatibilityLevel: (string)info.CompatibilityLevel,
                Collation: (string)info.Collation
            );

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            return Result.Failure<DatabaseHealthDto>($"Database connection failed: {ex.Message}");
        }
    }
}
