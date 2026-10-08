using InteAcc.Common.Application.Models.Auth;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Application.Abstractions.SystemAdmin;

public interface ICompanyService
{
    Task<Result<List<CompanyAccessDto>>> GetAccessibleCompaniesAsync(int userId, CancellationToken cancellationToken = default);
}
