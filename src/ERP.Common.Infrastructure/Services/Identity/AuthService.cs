using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Identity;
using ERP.Common.Application.Models.Auth;
using ERP.Common.Application.Security;
using ERP.Common.Domain.Common;
using ERP.Common.Domain.Entities.SystemAdmin;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Identity;

public class AuthService : IAuthService
{
    private readonly ErpDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        ErpDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>("Invalid username or password.");
        }

        // Determine real accessible companies
        var accessibleCompanies = await GetUserAccessibleCompanies(user.Id, cancellationToken);

        int? activeCompanyId = request.CompanyId;
        if (!activeCompanyId.HasValue)
        {
            activeCompanyId = user.DefaultCompanyId ?? accessibleCompanies.FirstOrDefault()?.Id;
        }
        else if (accessibleCompanies.Any() && !accessibleCompanies.Any(c => c.Id == activeCompanyId.Value))
        {
            return Result.Failure<LoginResponse>("User does not have access to the selected company.");
        }

        // Determine user roles and permissions
        var roles = await GetUserRolesAsync(user.Id, cancellationToken);
        var permissions = ResolvePermissions(roles);
        var accessibleCompanyIds = accessibleCompanies.Select(c => c.Id).ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, activeCompanyId, roles, permissions, accessibleCompanyIds);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userProfile = new UserProfileDto(
            Id: user.Id,
            UserCode: user.UserCode,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ActiveCompanyId: activeCompanyId,
            Roles: roles,
            Permissions: permissions,
            AccessibleCompanies: accessibleCompanies
        );

        return Result.Success(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresInMinutes: 60,
            User: userProfile
        ));
    }

    public async Task<Result<UserProfileDto>> GetCurrentUserProfileAsync(int userId, int? activeCompanyId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user == null)
        {
            return Result.Failure<UserProfileDto>("User not found.");
        }

        var accessibleCompanies = await GetUserAccessibleCompanies(user.Id, cancellationToken);
        var roles = await GetUserRolesAsync(user.Id, cancellationToken);
        var permissions = ResolvePermissions(roles);

        var profile = new UserProfileDto(
            Id: user.Id,
            UserCode: user.UserCode,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ActiveCompanyId: activeCompanyId ?? user.DefaultCompanyId,
            Roles: roles,
            Permissions: permissions,
            AccessibleCompanies: accessibleCompanies
        );

        return Result.Success(profile);
    }

    public async Task<Result<LoginResponse>> SwitchCompanyAsync(int userId, int targetCompanyId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user == null)
        {
            return Result.Failure<LoginResponse>("User not found.");
        }

        var accessibleCompanies = await GetUserAccessibleCompanies(user.Id, cancellationToken);
        if (accessibleCompanies.Any() && !accessibleCompanies.Any(c => c.Id == targetCompanyId))
        {
            return Result.Failure<LoginResponse>("User does not have access to the target company.");
        }

        var roles = await GetUserRolesAsync(user.Id, cancellationToken);
        var permissions = ResolvePermissions(roles);
        var accessibleCompanyIds = accessibleCompanies.Select(c => c.Id).ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, targetCompanyId, roles, permissions, accessibleCompanyIds);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var userProfile = new UserProfileDto(
            Id: user.Id,
            UserCode: user.UserCode,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ActiveCompanyId: targetCompanyId,
            Roles: roles,
            Permissions: permissions,
            AccessibleCompanies: accessibleCompanies
        );

        return Result.Success(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresInMinutes: 60,
            User: userProfile
        ));
    }

    private async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .Select(ur => ur.Role!.RoleCode)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
        {
            // Default role fallback for accounts without explicit role assignment
            roles.Add("SUPERADMIN");
        }

        return roles;
    }

    private static List<string> ResolvePermissions(IEnumerable<string> roles)
    {
        var isSuperAdmin = roles.Any(r => r.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase)
                                       || r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

        if (isSuperAdmin)
        {
            return Permissions.All.ToList();
        }

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            switch (role.ToUpperInvariant())
            {
                case "FINANCE_MGR":
                    permissions.Add(Permissions.GlAccountsView);
                    permissions.Add(Permissions.GlAccountsManage);
                    permissions.Add(Permissions.GlVouchersView);
                    permissions.Add(Permissions.GlVouchersCreate);
                    permissions.Add(Permissions.GlVouchersPost);
                    permissions.Add(Permissions.BcAccountsView);
                    permissions.Add(Permissions.BcTransactionsManage);
                    permissions.Add(Permissions.ApVendorsView);
                    permissions.Add(Permissions.ApVendorsManage);
                    permissions.Add(Permissions.ApBillsManage);
                    permissions.Add(Permissions.ArCustomersView);
                    permissions.Add(Permissions.ArInvoicesManage);
                    break;

                case "SALES_MGR":
                    permissions.Add(Permissions.ArCustomersView);
                    permissions.Add(Permissions.ArCustomersManage);
                    permissions.Add(Permissions.ArInvoicesManage);
                    permissions.Add(Permissions.SalesOrdersManage);
                    permissions.Add(Permissions.InvProductsView);
                    break;

                default:
                    permissions.Add(Permissions.SaCompaniesView);
                    break;
            }
        }

        return permissions.ToList();
    }

    private async Task<List<CompanyAccessDto>> GetUserAccessibleCompanies(int userId, CancellationToken cancellationToken)
    {
        var isSuperAdmin = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .AnyAsync(ur => ur.Role!.RoleCode == "SUPERADMIN" || ur.Role!.RoleCode == "SuperAdmin", cancellationToken);

        // If user is superadmin or has no explicit company access restrictions, grant access to active companies
        if (isSuperAdmin)
        {
            return await _dbContext.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Id)
                .Select(c => new CompanyAccessDto(
                    c.Id,
                    c.CompanyCode,
                    c.CompanyName,
                    c.BaseCurrency,
                    true
                ))
                .ToListAsync(cancellationToken);
        }

        var assignedCompanies = await _dbContext.UserCompanyAccess
            .Where(uca => uca.UserId == userId && uca.Company != null && uca.Company.IsActive)
            .OrderBy(uca => uca.Company!.Id)
            .Select(uca => new CompanyAccessDto(
                uca.Company!.Id,
                uca.Company.CompanyCode,
                uca.Company.CompanyName,
                uca.Company.BaseCurrency,
                true
            ))
            .ToListAsync(cancellationToken);

        // Fallback: If no companies explicitly assigned, provide default company
        if (assignedCompanies.Count == 0)
        {
            var defaultComp = await _dbContext.Companies.Where(c => c.IsActive).OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
            if (defaultComp != null)
            {
                assignedCompanies.Add(new CompanyAccessDto(
                    defaultComp.Id,
                    defaultComp.CompanyCode,
                    defaultComp.CompanyName,
                    defaultComp.BaseCurrency,
                    true
                ));
            }
        }

        return assignedCompanies;
    }
}
