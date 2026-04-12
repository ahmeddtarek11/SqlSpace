namespace SqlSpace.Domain.Enums;

public enum DbProviders
{
    SqlServer    = 1,
    PostgreSql   = 2,
    MySql        = 3,
    MariaDb      = 4,
    CockroachDb  = 5,
    Supabase     = 6,
    PlanetScale  = 7,
    Redshift     = 8,
}


public static class DbProvidersExtensions
{

    public static string GetDefaultSchema(this DbProviders provider)
    {
        return provider switch
        {
            DbProviders.PostgreSql  => "public",
            DbProviders.SqlServer   => "dbo",
            DbProviders.MySql       => "",
            DbProviders.MariaDb     => "",
            DbProviders.CockroachDb => "public",
            DbProviders.Supabase    => "public",
            DbProviders.PlanetScale => "",
            DbProviders.Redshift    => "public",
            _ => "public"
        };
    }

    public static bool SupportsSchemas(this DbProviders provider)
    {
        return provider switch
        {
            DbProviders.MySql       => false,
            DbProviders.MariaDb     => false,
            DbProviders.PlanetScale => false,
            _ => true
        };
    }

    public static int GetDefaultPort(this DbProviders provider)
    {
        return provider switch
        {
            DbProviders.PostgreSql  => 5432,
            DbProviders.SqlServer   => 1433,
            DbProviders.MySql       => 3306,
            DbProviders.MariaDb     => 3306,
            DbProviders.CockroachDb => 26257,
            DbProviders.Supabase    => 5432,
            DbProviders.PlanetScale => 3306,
            DbProviders.Redshift    => 5439,
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
