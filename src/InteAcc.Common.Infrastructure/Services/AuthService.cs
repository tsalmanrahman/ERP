using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Identity;
using InteAcc.Common.Application.Models.Auth;
using InteAcc.Common.Application.Security;
using InteAcc.Common.Domain.Common;
using InteAcc.Common.Domain.Entities.SystemAdmin;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly InteAccDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        InteAccDbContext dbContext,
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

        // Determine accessible companies
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

        var roles = new List<string> { "SuperAdmin" }; // Default initial role
        var permissions = Permissions.All.ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, activeCompanyId, roles, permissions);
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
        var roles = new List<string> { "SuperAdmin" };
        var permissions = Permissions.All.ToList();

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

        var roles = new List<string> { "SuperAdmin" };
        var permissions = Permissions.All.ToList();

        var accessToken = _jwtTokenService.GenerateAccessToken(user, targetCompanyId, roles, permissions);
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

    private async Task<List<CompanyAccessDto>> GetUserAccessibleCompanies(int userId, CancellationToken cancellationToken)
    {
        var allCompanies = await _dbContext.Companies
            .Where(c => c.IsActive)
            .Select(c => new CompanyAccessDto(
                c.Id,
                c.CompanyCode,
                c.CompanyName,
                c.BaseCurrency,
                false
            ))
            .ToListAsync(cancellationToken);

        return allCompanies;
    }
}
