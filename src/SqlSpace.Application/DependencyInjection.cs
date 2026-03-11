using Microsoft.Extensions.DependencyInjection;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.Abstractions.Query;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.Services.AccessControl;
using SqlSpace.Application.Services.Auth;
using SqlSpace.Application.Services.Connection;
using SqlSpace.Application.Services.Query;
using SqlSpace.Application.Services.schema;

namespace SqlSpace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IConnectionManagementService, ConnectionManagementService>();
        services.AddScoped<IQueryExecutionService, QueryExecutionService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        services.AddScoped<ISchemaContextService, SchemaContextService>();
        services.AddSingleton<ISqlValidator, SqlValidatorService>();
        return services;
    }
}
