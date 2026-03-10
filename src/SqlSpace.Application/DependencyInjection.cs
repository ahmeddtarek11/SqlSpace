using Microsoft.Extensions.DependencyInjection;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.Services.AccessControl;
using SqlSpace.Application.Services.Auth;
using SqlSpace.Application.Services.Connection;

namespace SqlSpace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IConnectionManagementService, ConnectionManagementService>();
        return services;
    }
}
