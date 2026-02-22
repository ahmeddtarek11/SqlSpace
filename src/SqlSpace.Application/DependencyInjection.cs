using Microsoft.Extensions.DependencyInjection;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Services.Auth;

namespace SqlSpace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
