using SqlSpace.Application.DTOs.Query;

namespace SqlSpace.Application.Abstractions.Security;

/// <summary>
/// Validates SQL queries for safety and authorization before execution.
/// </summary>
/// <remarks>
/// Usage:
/// - Called after AI generates SQL and before database execution.
/// - Provides defense-in-depth against malicious or unauthorized SQL.
///
/// When:
/// - Immediately after LLM returns generated SQL.
/// - Before any external database interaction.
/// - When re-running historical queries with updated permissions.
///
/// Why:
/// - Prevents data modification through INSERT/UPDATE/DELETE detection.
/// - Blocks dangerous DDL operations like DROP/TRUNCATE.
/// - Validates query only references authorized tables.
/// - Provides SQL injection defense layer.
///
/// Where:
/// - Interface consumed by query execution orchestration.
/// - Implemented in Application layer as a SQL safety/authorization validator.
///
/// How:
/// - Parse SQL using regex and keyword detection.
/// - Extract table references from FROM and JOIN clauses.
/// - Match extracted tables against user's permission list.
/// - Check for dangerous SQL keywords and patterns.
/// </remarks>
public interface ISqlValidator
{
    /// <summary>
    /// Validates SQL query meets all safety and authorization requirements.
    /// </summary>
    /// <param name="sql">Generated SQL query to validate.</param>
    /// <param name="accessibleTables">List of table names user is authorized to query.</param>
    /// <param name="cancellationToken">Cancellation token for validation operation.</param>
    /// <returns>Validation result with success flag, extracted tables, and error details.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Normalize SQL (trim, uppercase for analysis).
    /// 2. Check SQL starts with SELECT keyword.
    /// 3. Scan for dangerous keywords (INSERT, UPDATE, DELETE, DROP, TRUNCATE, ALTER, EXEC, etc.).
    /// 4. Extract table names from FROM and JOIN clauses using regex.
    /// 5. Handle schema-qualified names (schema.table).
    /// 6. Compare extracted tables against accessible tables list.
    /// 7. Identify any unauthorized table references.
    /// 8. Return validation result with detailed error if validation fails.
    /// </remarks>
    Task<SqlValidationResult> ValidateQueryAsync(
        string sql,
        IReadOnlyList<string> accessibleTables,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if SQL contains only SELECT statement (read-only).
    /// </summary>
    /// <param name="sql">SQL query to check.</param>
    /// <returns><c>true</c> if query is SELECT-only; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Trim and uppercase SQL.
    /// 2. Verify first non-whitespace token is SELECT.
    /// 3. Scan for write operations (INSERT, UPDATE, DELETE, MERGE).
    /// 4. Return boolean result.
    /// </remarks>
    bool IsSelectOnly(string sql);

    /// <summary>
    /// Extracts all table names referenced in SQL query.
    /// </summary>
    /// <param name="sql">SQL query to parse.</param>
    /// <returns>List of distinct table names (may include schema prefix).</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Apply regex patterns for FROM and JOIN clauses.
    /// 2. Extract table identifiers (handle aliases, schema names).
    /// 3. Normalize table names (lowercase, trim).
    /// 4. Return distinct list of referenced tables.
    /// </remarks>
    IReadOnlyList<string> ExtractTableNames(string sql);

    /// <summary>
    /// Scans SQL for dangerous keywords that could cause data modification or system damage.
    /// </summary>
    /// <param name="sql">SQL query to scan.</param>
    /// <returns><c>true</c> if dangerous keywords detected; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Define dangerous keyword list (INSERT, UPDATE, DELETE, DROP, TRUNCATE, ALTER, CREATE, EXEC, EXECUTE, etc.).
    /// 2. Scan SQL for keyword matches.
    /// 3. Return true if any dangerous keyword found.
    /// </remarks>
    bool ContainsDangerousKeywords(string sql);
}
