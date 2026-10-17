using ERP.Common.Application.Models;
using ERP.Common.Domain.Common;

namespace ERP.Common.Application.Abstractions.Data;

public interface IDatabaseHealthService
{
    Task<Result<DatabaseHealthDto>> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);
}
