using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Infrastructure.Connection;



/// <summary>
/// Connection string templates for each provider.
/// </summary>
internal static class ConnectionStringTemplates
{
    public static string GetTemplate(DbProviders provider) => provider switch
    {
        DbProviders.PostgreSql => "Host={0};Port={1};Database={2};Username={3};Password={4}",
        DbProviders.SqlServer => "Server={0},{1};Database={2};User Id={3};Password={4}",
        DbProviders.MySql => "Server={0};Port={1};Database={2};Uid={3};Pwd={4}",
        _ => throw new ArgumentException($"Unsupported provider: {provider}")
    };

    public static string GetSslParameter(DbProviders provider) => provider switch
    {
        DbProviders.PostgreSql => "SSL Mode=Require",
        DbProviders.SqlServer => "Encrypt=True;TrustServerCertificate=False",
        DbProviders.MySql => "SslMode=Required",
        _ => string.Empty
    };

    public static (string Host, string Port, string Database, string Username, string Password, string Ssl) GetParameterNames(DbProviders provider) => provider switch
    {
        DbProviders.PostgreSql => ("Host", "Port", "Database", "Username", "Password", "SSL Mode"),
        DbProviders.SqlServer => ("Server", "Server", "Database", "User Id", "Password", "Encrypt"),
        DbProviders.MySql => ("Server", "Port", "Database", "Uid", "Pwd", "SslMode"),
        _ => throw new ArgumentException($"Unsupported provider: {provider}")
    };
    
}
