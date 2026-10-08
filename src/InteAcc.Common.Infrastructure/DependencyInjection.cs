using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Data;
using InteAcc.Common.Application.Abstractions.Identity;
using InteAcc.Common.Application.Abstractions.SystemAdmin;
using InteAcc.Common.Infrastructure.Data;
using InteAcc.Common.Infrastructure.Services;

namespace InteAcc.Common.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();

        services.AddDbContext<InteAccDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<InteAccDbContext>());

        return services;
    }
}
