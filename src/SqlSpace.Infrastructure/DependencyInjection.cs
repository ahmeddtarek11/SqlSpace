using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SqlSpace.Application.Abstractions.AI;
using SqlSpace.Application.Abstractions.Audit;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Infrastructure.AI;
using SqlSpace.Infrastructure.AuditLog;
using SqlSpace.Infrastructure.Connection;
using SqlSpace.Infrastructure.Data;
using SqlSpace.Infrastructure.Identity;
using SqlSpace.Infrastructure.integration;
using SqlSpace.Infrastructure.Security;

namespace SqlSpace.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddHttpContextAccessor();
        services.Configure<llmApi>(configuration.GetSection("LlmApi"));
        services.AddHttpClient<ITextToSqlClient, TextToSqlClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<llmApi>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseLink) &&
                Uri.TryCreate(options.BaseLink, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            if (options.TimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            }
        });
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var JwtSettings = configuration.GetSection("JwtSettings");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidAudience = JwtSettings["Audience"],
                ValidIssuer = JwtSettings["Issuer"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings["Secret"]!))

            };
        });
        services.AddAuthorization();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 4 ;
                options.Password.RequireDigit = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireUppercase =false;
                options.SignIn.RequireConfirmedAccount =false;
                options.Password.RequireNonAlphanumeric =false;
                options.Password.RequireLowercase = false;

            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddDefaultTokenProviders();

        
            services.AddDataProtection();
            services.AddScoped<IEncryptionService,EncryptionService>();
            services.AddScoped<IJwtTokenProvider,JwtTokenProvider>();
            services.AddScoped<IAuthProvider, AuthProvider>();
            services.AddScoped<IRefreshTokenProvider, RefreshTokenProvider>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<IConnectionStringBuilder , ConnectionStringBuilderService>();
            services.AddScoped<IDatabaseExecutor, DatabaseExecutor>();
            services.AddScoped<ISchemaExtractor , SchemaExtractor>();

        return services;
    }
}
