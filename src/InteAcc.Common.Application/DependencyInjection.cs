using Microsoft.Extensions.DependencyInjection;

namespace InteAcc.Common.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonApplication(this IServiceCollection services)
    {
        // Application layer registrations (MediatR/Pipelines/Validators) will be wired here
        return services;
    }
}
