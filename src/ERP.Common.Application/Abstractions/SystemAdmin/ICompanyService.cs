using ERP.Common.Application.Models.Auth;
using ERP.Common.Domain.Common;

namespace ERP.Common.Application.Abstractions.SystemAdmin;

public interface ICompanyService
{
    Task<Result<List<CompanyAccessDto>>> GetAccessibleCompaniesAsync(int userId, CancellationToken cancellationToken = default);
}
