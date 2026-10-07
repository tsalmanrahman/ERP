using InteAcc.Common.Application.Models;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Application.Abstractions.Data;

public interface IDatabaseHealthService
{
    Task<Result<DatabaseHealthDto>> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);
}
