using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.SystemAdmin;
using ERP.Common.Application.Models.Auth;
using ERP.Common.Domain.Common;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.SystemAdmin;

public class CompanyService : ICompanyService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext? _currentUserContext;

    public CompanyService(ErpDbContext dbContext, ICurrentUserContext? currentUserContext = null)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<List<CompanyAccessDto>>> GetAccessibleCompaniesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var activeCompanyId = _currentUserContext?.CompanyId;

        var isSuperAdmin = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .AnyAsync(ur => ur.Role!.RoleCode == "SUPERADMIN" || ur.Role!.RoleCode == "SuperAdmin", cancellationToken);

        var hasAssignedCompanies = await _dbContext.UserCompanyAccess
            .AnyAsync(uca => uca.UserId == userId, cancellationToken);

        if (hasAssignedCompanies)
        {
            var assigned = await _dbContext.UserCompanyAccess
                .Where(uca => uca.UserId == userId && uca.Company != null && uca.Company.IsActive)
                .OrderBy(uca => uca.Company!.Id)
                .Select(uca => new CompanyAccessDto(
                    uca.Company!.Id,
                    uca.Company.CompanyCode,
                    uca.Company.CompanyName,
                    uca.Company.BaseCurrency,
                    activeCompanyId.HasValue ? uca.Company.Id == activeCompanyId.Value : uca.Company.Id == 1
                ))
                .ToListAsync(cancellationToken);

            return Result.Success(assigned);
        }

        if (isSuperAdmin)
        {
            var allCompanies = await _dbContext.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Id)
                .Select(c => new CompanyAccessDto(
                    c.Id,
                    c.CompanyCode,
                    c.CompanyName,
                    c.BaseCurrency,
                    activeCompanyId.HasValue ? c.Id == activeCompanyId.Value : c.Id == 1
                ))
                .ToListAsync(cancellationToken);

            return Result.Success(allCompanies);
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user?.DefaultCompanyId is > 0)
        {
            var defaultCompanies = await _dbContext.Companies
                .Where(c => c.IsActive && c.Id == user.DefaultCompanyId.Value)
                .Select(c => new CompanyAccessDto(
                    c.Id,
                    c.CompanyCode,
                    c.CompanyName,
                    c.BaseCurrency,
                    true
                ))
                .ToListAsync(cancellationToken);

            return Result.Success(defaultCompanies);
        }

        return Result.Success(new List<CompanyAccessDto>());
    }
}
