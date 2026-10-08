using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.SystemAdmin;
using InteAcc.Common.Application.Models.Auth;
using InteAcc.Common.Domain.Common;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly InteAccDbContext _dbContext;

    public CompanyService(InteAccDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<CompanyAccessDto>>> GetAccessibleCompaniesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var companies = await _dbContext.Companies
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyCode)
            .Select(c => new CompanyAccessDto(
                c.Id,
                c.CompanyCode,
                c.CompanyName,
                c.BaseCurrency,
                false
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(companies);
    }
}
