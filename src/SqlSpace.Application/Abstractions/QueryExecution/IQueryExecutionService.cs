using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Query;

/// <summary>
/// Orchestrates the complete natural-language-to-SQL query execution pipeline.
/// </summary>
/// <remarks>
/// Usage:
/// - Primary entry point for all query execution requests from API controllers.
/// - Coordinates schema filtering, LLM invocation, SQL validation, and external DB execution.
///
/// When:
/// - User submits natural language prompt through the query endpoint.
/// - User requests to re-run a previously executed query from history.
///
/// Why:
/// - Centralizes complex multi-step query workflow in a single orchestration service.
/// - Enforces consistent authorization checks and audit logging across all query paths.
///
/// Where:
/// - Interface consumed by API controllers and application layer.
/// - Implemented in Application layer as a query-execution orchestration service.
///
/// How:
/// - Validate user access to connection.
/// - Retrieve filtered schema based on permissions.
/// - Send prompt + schema to AI service.
/// - Validate generated SQL.
/// - Execute on external database.
/// - Persist to query history with snapshot.
/// </remarks>
public interface IQueryExecutionService
{
    /// <summary>
    /// Executes a natural language prompt through the full text-to-SQL pipeline.
    /// </summary>
    /// <param name="connectionId">Target database connection identifier.</param>
    /// <param name="userId">Authenticated user identifier from JWT claims.</param>
    /// <param name="userPrompt">Natural language query input from user.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Complete query execution result including SQL, results, and execution metadata.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate connection exists and is not deleted.
    /// 2. Verify user has active access grant (admin or explicit grant).
    /// 3. Retrieve user's accessible tables (full or filtered by restrictions).
    /// 4. Get filtered schema context for LLM.(validate a schema exists , if not , exctract the schema and presist in db)
    /// 5. Send prompt + schema to AI service for SQL generation.
    /// 6. Validate generated SQL (SELECT-only, authorized tables).
    /// 7. Execute SQL on external database using connection factory.
    /// 8. Capture results and execution time.
    /// 9. Persist complete query record to history with permission snapshot.
    /// 10. Return result to caller.
    /// </remarks>
    Task<Result<QueryExecutionResult>> ExecutePromptAsync(
        Guid connectionId,
        string userId,
        string userPrompt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a pre-generated SQL query through the validation and execution pipeline.
    /// </summary>
    /// <param name="connectionId">Target database connection identifier.</param>
    /// <param name="userId">Authenticated user identifier from JWT claims.</param>
    /// <param name="userPrompt">Original user prompt or label for history.</param>
    /// <param name="generatedSql">SQL statement to validate and execute.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Complete query execution result including SQL, results, and execution metadata.</returns>
    Task<Result<QueryExecutionResult>> ExecuteSqlAsync(
        Guid connectionId,
        string userId,
        string userPrompt,
        string generatedSql,
        CancellationToken cancellationToken);

    /// <summary>
    /// Re-executes a previously run query from history with current user permissions.
    /// </summary>
    /// <param name="queryId">Query history record identifier to re-run.</param>
    /// <param name="userId">Authenticated user identifier from JWT claims.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>New query execution result with updated permissions check.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load original query from history.
    /// 2. Verify caller is the original user or connection admin.
    /// 3. Validate user still has access to the connection.
    /// 4. Check current table permissions (may have changed since original).
    /// 5. Validate SQL against current permissions.
    /// 6. Execute SQL on external database.
    /// 7. Create new query history record (separate from original).
    /// 8. Return updated result.
    /// </remarks>
    Task<Result<QueryExecutionResult>> RerunQueryAsync(
        Guid queryId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves query execution history for a user with optional filtering.
    /// </summary>
    /// <param name="userId">User identifier to retrieve history for.</param>
    /// <param name="connectionId">Optional connection filter (null for all connections).</param>
    /// <param name="pageNumber">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Paginated list of query history records.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query history by user id.
    /// 2. Apply connection filter if provided.
    /// 3. Order by execution time descending (newest first).
    /// 4. Apply pagination.
    /// 5. Return page result with total count.
    /// </remarks>
    Task<Result<PaginatedQueryHistory>> GetUserQueryHistoryAsync(
        string userId,
        Guid? connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
