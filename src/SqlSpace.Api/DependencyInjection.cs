using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SqlSpace.Api.common;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;

namespace SqlSpace.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {



        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();






        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SqlSpace API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Paste JWT only. 'Bearer ' is added automatically.",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

      
});








        return services;
    }
}
