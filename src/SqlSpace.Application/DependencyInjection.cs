using Microsoft.Extensions.DependencyInjection;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Analytics;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.Abstractions.Insights;
using SqlSpace.Application.Abstractions.Query;
using SqlSpace.Application.Abstractions.SavedQueries;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.Services.AccessControl;
using SqlSpace.Application.Services.Analytics;
using SqlSpace.Application.Services.Auth;
using SqlSpace.Application.Services.Connection;
using SqlSpace.Application.Services.Insights;
using SqlSpace.Application.Services.Query;
using SqlSpace.Application.Services.schema;
using SqlSpace.Application.Services.SavedQueries;
using SqlSpace.Application.Abstractions.KnowledgeBase;
using SqlSpace.Application.Services.KnowledgeBase;

namespace SqlSpace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IConnectionManagementService, ConnectionManagementService>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<IQueryExecutionService, QueryExecutionService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        services.AddScoped<ISavedQueryService, SavedQueryService>();
        services.AddScoped<ISchemaContextService, SchemaContextService>();
        services.AddSingleton<ISqlValidator, SqlValidatorService>();
        services.AddScoped<IChartService, ChartService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        return services;
    }
}
