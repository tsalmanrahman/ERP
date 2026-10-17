using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.SystemAdmin;
using ERP.Common.Application.Models.Auth;
using ERP.Common.Domain.Common;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.SystemAdmin;

public class CompanyService : ICompanyService
{
    private readonly ErpDbContext _dbContext;

    public CompanyService(ErpDbContext dbContext)
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
