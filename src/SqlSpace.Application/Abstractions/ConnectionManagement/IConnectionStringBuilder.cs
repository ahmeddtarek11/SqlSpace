using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Abstractions.Integrations;

/// <summary>
/// Builds and parses provider-specific database connection strings.
/// </summary>
/// <remarks>
/// Usage:
/// - Used when creating connections from individual components (host, port, username, etc.).
/// - Used to parse raw connection strings into components for validation and display.
///
/// When:
/// - User submits connection via simple form mode (individual fields).
/// - System needs to construct connection string for external database access.
/// - Parsing advanced mode connection strings for validation.
///
/// Why:
/// - Encapsulates provider-specific connection string formats.
/// - Supports multiple database providers with consistent interface.
/// - Enables switching between simple and advanced connection modes.
///
/// Where:
/// - Interface consumed by connection management and validation services.
/// - Implemented in Infrastructure integration layer.
///
/// How:
/// - Build connection strings following provider conventions.
/// - Parse connection strings using provider-specific rules.
/// - Handle SSL/TLS options and additional parameters.
/// </remarks>
public interface IConnectionStringBuilder
{
    /// <summary>
    /// Builds a complete connection string from individual components.
    /// </summary>
    /// <param name="provider">Database provider type (PostgreSQL, SqlServer, MySql).</param>
    /// <param name="host">Database server hostname or IP address.</param>
    /// <param name="port">Database server port number.</param>
    /// <param name="database">Target database name.</param>
    /// <param name="username">Database authentication username.</param>
    /// <param name="password">Database authentication password (plain text, not encrypted).</param>
    /// <param name="useSSL">Whether to enable SSL/TLS encryption for connection.</param>
    /// <param name="additionalParameters">Optional provider-specific connection parameters.</param>
    /// <returns>Formatted connection string ready for use with ADO.NET providers.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate all required components are provided.
    /// 2. Select connection string format based on provider.
    /// 3. Assemble base connection string (host, port, database, credentials).
    /// 4. Add SSL/TLS configuration based on provider and useSSL flag.
    /// 5. Append additional parameters if provided.
    /// 6. Return complete connection string.
    ///
    /// Provider-specific formats:
    /// - PostgreSQL: "Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require"
    /// - SqlServer: "Server={host},{port};Database={db};User Id={user};Password={pass};Encrypt=True"
    /// - MySql: "Server={host};Port={port};Database={db};Uid={user};Pwd={pass};SslMode=Required"
    /// </remarks>
    string BuildConnectionString(
        DbProviders provider,
        string host,
        int port,
        string database,
        string username,
        string password,
        bool useSSL,
        string? additionalParameters);

    /// <summary>
    /// Parses a connection string into individual components for validation or display.
    /// </summary>
    /// <param name="connectionString">Raw connection string to parse.</param>
    /// <param name="provider">Database provider type for parsing rules.</param>
    /// <returns>Parsed connection components or null if parsing fails.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Split connection string by semicolon delimiter.
    /// 2. Parse key-value pairs (key=value format).
    /// 3. Extract provider-specific parameter names:
    ///    - PostgreSQL: Host, Port, Database, Username
    ///    - SqlServer: Server, Database, User Id
    ///    - MySql: Server, Port, Database, Uid
    /// 4. Handle server format (host:port or host,port).
    /// 5. Detect SSL usage from connection parameters.
    /// 6. Return structured component object.
    /// 7. Return null if parsing fails or required components missing.
    /// </remarks>
    ConnectionComponents? ParseConnectionString(
        string connectionString,
        DbProviders provider);

    /// <summary>
    /// Validates a connection string format for a specific provider.
    /// </summary>
    /// <param name="connectionString">Connection string to validate.</param>
    /// <param name="provider">Database provider type.</param>
    /// <returns>Validation result with success flag and error details.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Attempt to parse connection string.
    /// 2. Verify all required components are present (host, database, username).
    /// 3. Validate port is in valid range (1-65535).
    /// 4. Check for security issues (plain text passwords exposed in logs).
    /// 5. Return validation result with specific error messages.
    /// </remarks>
    ConnectionStringValidationResult ValidateConnectionString(
        string connectionString,
        DbProviders provider);

  
}
