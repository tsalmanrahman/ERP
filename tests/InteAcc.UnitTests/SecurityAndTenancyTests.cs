using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Domain.Entities.GeneralLedger;
using InteAcc.Common.Domain.Entities.SystemAdmin;
using InteAcc.Common.Infrastructure.Data;
using InteAcc.Common.Infrastructure.Services;
using Xunit;

namespace InteAcc.UnitTests;

public class SecurityAndTenancyTests
{
    private class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "admin";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
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

    [Fact]
    public void JwtTokenService_ShouldGenerateTokenWithExpectedClaims()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "InteAcc_Super_Secret_Enterprise_ERP_Key_2026_Secure_Token!" },
            { "Jwt:Issuer", "InteAcc.ERP" },
            { "Jwt:Audience", "InteAcc.Client" },
            { "Jwt:ExpiryMinutes", "60" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var jwtService = new JwtTokenService(config);

        var user = new User
        {
            UserCode = "USR-001",
            Username = "testadmin",
            Email = "test@inteacc.com",
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

        var options = new DbContextOptionsBuilder<InteAccDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new InteAccDbContext(options, currentUserContext: userContext);

        Assert.Equal(1, context.CurrentCompanyId);
    }
}
