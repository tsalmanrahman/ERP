using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Security;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.SystemAdmin;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Common;
using ERP.Common.Infrastructure.Services.Identity;
using ERP.Common.Application.Models.Auth;
using Xunit;

namespace ERP.UnitTests;

public class SecurityAndTenancyTests
{
    private class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "admin";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private static void SetEntityId<T>(T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id")?.SetValue(entity, id);
    }

    [Fact]
    public void PasswordHasher_ShouldHashAndVerifySuccessfully()
    {
        var hasher = new PasswordHasher();
        const string password = "TestPassword@2026";

        var hash = hasher.HashPassword(password);
        Assert.NotNull(hash);
        Assert.Contains(".", hash);

        Assert.True(hasher.VerifyPassword(password, hash));
        Assert.False(hasher.VerifyPassword("WrongPassword", hash));
    }

    private static JwtTokenService CreateJwtService() => new(Options.Create(new JwtSettings
    {
        SecretKey = "unit-test-signing-key-0123456789-abcdefghij",
        Issuer = "ERP.Security",
        Audience = "ERP.Client",
        ExpiryMinutes = 60
    }));

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void JwtTokenService_ShouldRejectMissingOrShortSigningKey(string secret)
    {
        var options = Options.Create(new JwtSettings { SecretKey = secret });

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(options));
    }

    [Fact]
    public void JwtTokenService_ShouldGenerateTokenWithExpectedClaims()
    {
        var jwtService = CreateJwtService();

        var user = new User
        {
            UserCode = "USR-001",
            Username = "testadmin",
            Email = "test@erp.local",
            FullName = "Test Admin"
        };
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, 10);

        var tokenString = jwtService.GenerateAccessToken(
            user,
            companyId: 2,
            roles: new[] { "SuperAdmin" },
            permissions: new[] { "Permissions.Gl.Accounts.View" }
        );

        Assert.NotNull(tokenString);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("10", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("testadmin", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("2", token.Claims.First(c => c.Type == "company_id").Value);
        Assert.Contains(token.Claims, c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == "SuperAdmin");
        Assert.Contains(token.Claims, c => c.Type == "permission" && c.Value == "Permissions.Gl.Accounts.View");
    }

    [Fact]
    public void MultiTenantQueryFilter_ShouldFilterRecordsByCurrentCompany()
    {
        var userContext = new FakeUserContext { CompanyId = 1 };

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new ErpDbContext(options, currentUserContext: userContext);

        Assert.Equal(1, context.CurrentCompanyId);
    }

    [Fact]
    public void CurrentUserContext_ShouldPreventTenantSpoofing()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("unique_name", "standard_user"),
            new Claim("company_id", "1"),
            new Claim(ClaimTypes.Role, "StandardUser")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        // Attacker attempts to spoof company 99 via header
        httpContext.Request.Headers["X-Company-Id"] = "99";

        var contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
        var userContext = new CurrentUserContext(contextAccessor);

        // Security assertion: spoofed tenant must be rejected and fallback to token's company 1
        Assert.Equal(1, userContext.CompanyId);
        Assert.False(userContext.HasAccessToCompany(99));
        Assert.True(userContext.HasAccessToCompany(1));
    }

    [Fact]
    public void CurrentUserContext_ShouldAllowTenantSwitchForSuperAdmin()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim("unique_name", "admin"),
            new Claim("company_id", "1"),
            new Claim(ClaimTypes.Role, "SUPERADMIN")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContext.Request.Headers["X-Company-Id"] = "2";

        var contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
        var userContext = new CurrentUserContext(contextAccessor);

        // SuperAdmin is permitted to switch tenant via header
        Assert.Equal(2, userContext.CompanyId);
        Assert.True(userContext.HasAccessToCompany(2));
    }

    [Fact]
    public void CurrentUserContext_ShouldAllowAccessToPermittedAccessibleCompany()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim("unique_name", "finance_user"),
            new Claim("company_id", "1"),
            new Claim(ClaimTypes.Role, "FINANCE_MGR"),
            new Claim("accessible_company", "1"),
            new Claim("accessible_company", "3")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContext.Request.Headers["X-Company-Id"] = "3";

        var contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
        var userContext = new CurrentUserContext(contextAccessor);

        // Company 3 is in accessible_company claims, so header switch is permitted
        Assert.Equal(3, userContext.CompanyId);
        Assert.True(userContext.HasAccessToCompany(3));
        Assert.False(userContext.HasAccessToCompany(4)); // Company 4 not accessible
    }

    [Fact]
    public async Task AuthService_ShouldAuthenticateAndAssignRolesAndPermissions()
    {
        var dbName = "AuthService_TestDb_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var hasher = new PasswordHasher();
        var jwtService = CreateJwtService();

        using (var db = new ErpDbContext(options))
        {
            var company1 = new Company { CompanyCode = "C1", CompanyName = "Company 1", BaseCurrency = "BDT", IsActive = true };
            var company2 = new Company { CompanyCode = "C2", CompanyName = "Company 2", BaseCurrency = "USD", IsActive = true };
            SetEntityId(company1, 1);
            SetEntityId(company2, 2);
            db.Companies.AddRange(company1, company2);

            var user = new User
            {
                UserCode = "USR-FIN",
                Username = "finance_mgr",
                Email = "fin@erp.local",
                PasswordHash = hasher.HashPassword("Pass@123"),
                FullName = "Finance Manager",
                DefaultCompanyId = 1,
                IsActive = true
            };
            SetEntityId(user, 10);
            db.Users.Add(user);

            var role = new Role { RoleCode = "FINANCE_MGR", RoleName = "Finance Manager" };
            SetEntityId(role, 2);
            db.Roles.Add(role);
            db.UserRoles.Add(new UserRole { UserId = 10, RoleId = 2 });
            db.UserCompanyAccess.Add(new UserCompanyAccess { UserId = 10, CompanyId = 1 });

            await db.SaveChangesAsync();
        }

        using (var db = new ErpDbContext(options))
        {
            var authService = new AuthService(db, hasher, jwtService);
            var result = await authService.LoginAsync(new LoginRequest("finance_mgr", "Pass@123", null));

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.AccessToken);
            Assert.Equal("finance_mgr", result.Value.User.Username);
            Assert.Contains("FINANCE_MGR", result.Value.User.Roles);
            Assert.Contains(Permissions.GlVouchersCreate, result.Value.User.Permissions);
            Assert.DoesNotContain(Permissions.SaUsersManage, result.Value.User.Permissions); // Not SuperAdmin
            Assert.Single(result.Value.User.AccessibleCompanies);
            Assert.Equal(1, result.Value.User.AccessibleCompanies.First().Id);

            // Switching to an unauthorized company (Company 2) must fail
            var switchResult = await authService.SwitchCompanyAsync(10, 2);
            Assert.True(switchResult.IsFailure);
        }
    }
}
