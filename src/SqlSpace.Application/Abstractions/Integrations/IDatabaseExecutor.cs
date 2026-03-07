using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Integrations;

/// <summary>
/// Executes validated SQL queries against external user-managed databases.
/// </summary>
/// <remarks>
/// Usage:
/// - Called by query execution service after SQL generation and validation.
/// - Handles provider-specific query execution across PostgreSQL/SQL Server/MySQL.
///
/// When:
/// - After AI-generated SQL has been validated for safety and authorization.
/// - When re-running queries from history.
///
/// Why:
/// - Encapsulates database-specific query execution logic.
/// - Provides consistent result format across different database providers.
/// - Enforces read-only operations and timeout policies.
///
/// Where:
/// - Interface consumed by query execution workflows.
/// - Implemented in Infrastructure integration layer.
///
/// How:
/// - Use connection factory to get open database connection.
/// - Execute SELECT query with configured timeout.
/// - Transform result set to JSON-serializable format.
/// - Handle provider-specific data type conversions.
/// </remarks>
public interface IDatabaseSqlExecutor
{
    /// <summary>
    /// Executes a SELECT query on an external database and returns results as structured data.
    /// </summary>
    /// <param name="connection">Target database connection metadata and credentials.</param>
    /// <param name="sql">Validated SELECT SQL query to execute.</param>
    /// <param name="cancellationToken">Cancellation token for query timeout and cancellation.</param>
    /// <returns>Query execution result containing rows, execution time, and any errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate SQL is non-empty and connection is valid.
    /// 2. Create and open database connection via factory.
    /// 3. Prepare command with configured timeout (default 30 seconds).
    /// 4. Execute query and capture start time.
    /// 5. Read result set into memory-safe structure.
    /// 6. Transform data types for JSON serialization compatibility.
    /// 7. Calculate execution duration.
    /// 8. Serialize results to JSON.
    /// 9. Close and dispose connection.
    /// 10. Return structured result with metadata.
    /// </remarks>
    Task<DatabaseQueryResult> ExecuteQueryAsync(
        ConnectedDatabase connection,
        string sql,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tests database connectivity without executing any queries.
    /// </summary>
    /// <param name="connection">Target database connection metadata and credentials.</param>
    /// <param name="cancellationToken">Cancellation token with shorter timeout for connection test.</param>
    /// <returns>Connection test result with success flag and server metadata.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate connection metadata is complete.
    /// 2. Create and open connection with 10-second timeout.
    /// 3. Query server version/metadata.
    /// 4. Capture connection latency.
    /// 5. Close connection.
    /// 6. Return test result with server info.
    /// </remarks>
    Task<ConnectionTestResult> TestConnectionAsync(
        ConnectedDatabase connection,
        CancellationToken cancellationToken);
}
