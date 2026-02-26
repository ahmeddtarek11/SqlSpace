namespace SqlSpace.Domain.Enums;

public enum DbProviders
{
    SqlServer = 1 ,
    PostgreSql =2 , 
    MySql = 3 ,
    
    
}


public static class DbProvidersExtensions
{

    public static string GetDefaultSchema(this DbProviders provider)
    {
        return provider switch
        {
            DbProviders.PostgreSql => "public",
            DbProviders.SqlServer => "dbo",
            DbProviders.MySql => "",
            _ => "public"  // Fallback for unknown providers
        };
    }

     public static bool SupportsSchemas(this DbProviders provider)
    {
        return provider switch
        {
            DbProviders.MySql => false,
            _ => true
        };
    }

    public static int GetDefaultPort(this DbProviders provider)
    {
        return provider switch
        {
            DbProviders.PostgreSql => 5432,
            DbProviders.SqlServer => 1433,
            DbProviders.MySql => 3306,
            _ => 5432
        };
    }
    public static string BuildTableKey(this DbProviders provider, string tableName, string? schemaName)
    {
        var table = tableName?.Trim() ?? string.Empty;
        var schema = provider.NormalizeSchema(schemaName);

        return provider.SupportsSchemas()
            ? $"{schema}.{table}"
            : table;
    }
     public static string NormalizeSchema(this DbProviders provider, string? schema)
    {
        if (!provider.SupportsSchemas())
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(schema)
            ? provider.GetDefaultSchema()
            : schema.Trim();
    }
   
}
