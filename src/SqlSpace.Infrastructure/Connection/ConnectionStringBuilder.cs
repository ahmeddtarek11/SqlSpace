using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Infrastructure.Connection;


public class ConnectionStringBuilderService : IConnectionStringBuilder
{
     private readonly ILogger<ConnectionStringBuilderService> _logger;

    public ConnectionStringBuilderService(ILogger<ConnectionStringBuilderService> logger)
    {
        _logger = logger;
    }

    public string BuildConnectionString(
        DbProviders provider,
        string host,
        int port,
        string database,
        string username,
        string password,
        bool useSSL,
        string? additionalParameters)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be empty", nameof(host));
        if (port < 1 || port > 65535)
            throw new ArgumentException($"Port must be 1-65535, got: {port}", nameof(port));
        if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("Database cannot be empty", nameof(database));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        //  Escape special characters
        var escapedHost = EscapeValue(host);
        var escapedDatabase = EscapeValue(database);
        var escapedUsername = EscapeValue(username);
        var escapedPassword = EscapeValue(password);

        // Build base connection string
        var template = ConnectionStringTemplates.GetTemplate(provider);
        var connectionString = string.Format(
            template, 
            escapedHost, 
            port, 
            escapedDatabase, 
            escapedUsername, 
            escapedPassword);

        // Add SSL
        if (useSSL)
        {
            var sslParam = ConnectionStringTemplates.GetSslParameter(provider);
            if (!string.IsNullOrWhiteSpace(sslParam))
                connectionString += $";{sslParam}";
        }

        // Trim both sides of additional parameters
        if (!string.IsNullOrWhiteSpace(additionalParameters))
        {
            var cleaned = additionalParameters.Trim().Trim(';');
            if (!string.IsNullOrEmpty(cleaned))
                connectionString += $";{cleaned}";
        }

        return connectionString;
    }

    public ConnectionComponents? ParseConnectionString(
        string connectionString, 
        DbProviders provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        try
        {
            // Parse key-value pairs
            var parameters = ParseKeyValuePairs(connectionString);
            var paramNames = ConnectionStringTemplates.GetParameterNames(provider);

            // Extract host and port
            if (!TryExtractHostAndPort(parameters, paramNames, provider, out var host, out var port))
            {
                _logger.LogWarning(
                    "Failed to extract host/port from connection string for provider {Provider}", 
                    provider);
                return null;
            }

            // Extract required fields
            if (!parameters.TryGetValue(paramNames.Database, out var database) ||
                !parameters.TryGetValue(paramNames.Username, out var username))
            {
                _logger.LogWarning(
                    "Missing required fields (database or username) in connection string for provider {Provider}", 
                    provider);
                return null;
            }

            // Extract optional password
            parameters.TryGetValue(paramNames.Password, out var password);

            // Detect SSL
            var useSSL = DetectSsl(parameters, paramNames.Ssl, provider);

            // Extract additional parameters
            var coreParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                paramNames.Host, paramNames.Port, paramNames.Database,
                paramNames.Username, paramNames.Password, paramNames.Ssl
            };
            
            if (provider == DbProviders.SqlServer)
                coreParams.Add("Server");

            var additional = string.Join(";",
                parameters.Where(kvp => !coreParams.Contains(kvp.Key))
                          .Select(kvp => $"{kvp.Key}={kvp.Value}"));

            return new ConnectionComponents
            {
                Host = host,
                Port = port,
                DatabaseName = database,
                Username = username,
                Password = password ?? string.Empty,
                UseSSL = useSSL,
                AdditionalParameters = additional
            };
        }
        catch (Exception ex)
        {
            // ✅ FIX 4: Log exceptions instead of swallowing
            _logger.LogWarning(
                ex,
                "Failed to parse connection string for provider {Provider}. ConnectionString length: {Length}",
                provider,
                connectionString?.Length ?? 0);
            return null;
        }
    }

    public ConnectionStringValidationResult ValidateConnectionString(
        string connectionString, 
        DbProviders provider)
    {
        var components = ParseConnectionString(connectionString, provider);

        if (components == null)
            return new ConnectionStringValidationResult
            {
                IsValid = false,
                ErrorMessage = "Failed to parse connection string. Check format.",
                ParsedComponents = null  
            };

        if (string.IsNullOrWhiteSpace(components.Host))
            return new ConnectionStringValidationResult
            {
                IsValid = false,
                ErrorMessage = "Host is required.",
                ParsedComponents = null  
            };

        if (components.Port < 1 || components.Port > 65535)
            return new ConnectionStringValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Port must be 1-65535. Got: {components.Port}",
                ParsedComponents = null  
            };

        if (string.IsNullOrWhiteSpace(components.DatabaseName))
            return new ConnectionStringValidationResult
            {
                IsValid = false,
                ErrorMessage = "Database name is required.",
                ParsedComponents = null 
            };

        if (string.IsNullOrWhiteSpace(components.Username))
            return new ConnectionStringValidationResult
            {
                IsValid = false,
                ErrorMessage = "Username is required.",
                ParsedComponents = null  
            };

        return new ConnectionStringValidationResult 
        { 
            IsValid = true,
            ErrorMessage = null,
            ParsedComponents = components  // ✅ FIX 3: Populate on success!
        };
    }











//-----------------------------------------------------------------------------------------------------------------------------//



private static string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        
        // If value contains special chars, wrap in quotes and escape internal quotes
        if (value.Contains(';') || value.Contains('=') || value.Contains('"'))
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
        
        return value;
    }



    // Helper: Parse connection string into key-value dictionary
    private static Dictionary<string, string> ParseKeyValuePairs(string connectionString)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Split by semicolon, ignoring semicolons inside quotes
        var parts = Regex.Split(connectionString, ";(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            var equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex <= 0)
                continue;

            var key = trimmed.Substring(0, equalsIndex).Trim();
            var value = trimmed.Substring(equalsIndex + 1).Trim().Trim('"');
            parameters[key] = value;
        }

        return parameters;
    }










    // Helper: Extract host and port (handles SQL Server's combined format)
    private static bool TryExtractHostAndPort(
        Dictionary<string, string> parameters,
        (string Host, string Port, string Database, string Username, string Password, string Ssl) paramNames,
        DbProviders provider,
        out string host,
        out int port)
    {
        host = string.Empty;
        port = 0;

        if (provider == DbProviders.SqlServer)
        {
            // SQL Server: "Server=hostname,port" or "Server=hostname"
            if (!parameters.TryGetValue(paramNames.Host, out var server))
                return false;

            var parts = server.Split(',');
            host = parts[0].Trim();
            port = parts.Length > 1 && int.TryParse(parts[1], out var p)
                ? p
                : provider.GetDefaultPort();

            return true;
        }

        // PostgreSQL/MySQL: separate Host and Port
        if (!parameters.TryGetValue(paramNames.Host, out host!))
            return false;

        port = parameters.TryGetValue(paramNames.Port, out var portStr) && int.TryParse(portStr, out var parsedPort)
            ? parsedPort
            : provider.GetDefaultPort();

        return true;
    }














    

    // Helper: Detect if SSL is enabled
    private static bool DetectSsl(Dictionary<string, string> parameters, string sslParamName, DbProviders provider)
    {
        if (!parameters.TryGetValue(sslParamName, out var sslValue))
            return false;

        return provider switch
        {
            DbProviders.PostgreSql => sslValue.Equals("Require", StringComparison.OrdinalIgnoreCase) ||
                                      sslValue.Equals("VerifyCA", StringComparison.OrdinalIgnoreCase) ||
                                      sslValue.Equals("VerifyFull", StringComparison.OrdinalIgnoreCase),
            
            DbProviders.SqlServer => sslValue.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                                     sslValue.Equals("Mandatory", StringComparison.OrdinalIgnoreCase),
            
            DbProviders.MySql => sslValue.Equals("Required", StringComparison.OrdinalIgnoreCase) ||
                                 sslValue.Equals("VerifyCA", StringComparison.OrdinalIgnoreCase) ||
                                 sslValue.Equals("VerifyFull", StringComparison.OrdinalIgnoreCase),
            
            _ => false
        };
    }
}

