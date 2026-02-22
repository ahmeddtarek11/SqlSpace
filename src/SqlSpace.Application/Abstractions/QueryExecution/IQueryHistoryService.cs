using SqlSpace.Application.DTOs.Query;

namespace SqlSpace.Application.Abstractions.Query;

/// <summary>
/// Manages query history operations including retrieval, filtering, and export.
/// </summary>
/// <remarks>
/// Usage:
/// - Used to retrieve past queries for users and admins.
/// - Provides query analytics and audit capabilities.
///
/// When:
/// - User views their query history in UI.
/// - Admin audits all queries on their database connection.
/// - User exports query history for reporting.
///
/// Why:
/// - Separates query history concerns from execution logic.
/// - Provides consistent history access patterns.
/// - Enables efficient pagination and filtering.
///
/// Where:
/// - Interface consumed by API controllers and reporting services.
/// - Implemented in Application layer as a query-history use-case service.
///
/// How:
/// - Query QueryHistory entity with appropriate filters.
/// - Apply user-based authorization (users see own, admins see all).
/// - Support pagination and date range filtering.
/// </remarks>
public interface IQueryHistoryService
{
    /// <summary>
    /// Retrieves paginated query history for the current user.
    /// </summary>
    /// <param name="userId">User identifier to retrieve history for.</param>
    /// <param name="connectionId">Optional connection filter (null for all connections).</param>
    /// <param name="pageNumber">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of records per page (max 100).</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Paginated query history result with total count.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query QueryHistory by user id.
    /// 2. Apply connection filter if provided.
    /// 3. Order by ExecutedAt descending (newest first).
    /// 4. Calculate skip/take for pagination.
    /// 5. Include connection details for display.
    /// 6. Count total matching records.
    /// 7. Return page result with metadata.
    /// </remarks>
    Task<PaginatedQueryHistory> GetUserQueryHistoryAsync(
        string userId,
        Guid? connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all query history for a specific database connection (admin-only).
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="requestingUserId">User identifier requesting history (must be connection admin).</param>
    /// <param name="pageNumber">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Paginated query history for all users on this connection.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection and verify requesting user is admin.
    /// 2. Query all QueryHistory records for connection.
    /// 3. Order by ExecutedAt descending.
    /// 4. Include user details (who ran each query).
    /// 5. Apply pagination.
    /// 6. Return page result.
    /// </remarks>
    Task<PaginatedQueryHistory> GetConnectionQueryHistoryAsync(
        Guid connectionId,
        string requestingUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single query history record by identifier.
    /// </summary>
    /// <param name="queryId">Query history record identifier.</param>
    /// <param name="requestingUserId">User identifier requesting the query.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Query history details or null if not found/unauthorized.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load QueryHistory by id.
    /// 2. Check if requesting user is owner or connection admin.
    /// 3. Include connection details.
    /// 4. Return full query details or null if unauthorized.
    /// </remarks>
    Task<QueryHistoryDetailDto?> GetQueryByIdAsync(
        Guid queryId,
        string requestingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves query execution statistics for a user.
    /// </summary>
    /// <param name="userId">User identifier to get statistics for.</param>
    /// <param name="connectionId">Optional connection filter.</param>
    /// <param name="dateFrom">Optional start date for statistics window.</param>
    /// <param name="dateTo">Optional end date for statistics window.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Query execution statistics including counts and averages.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query QueryHistory by user id and optional filters.
    /// 2. Calculate total query count.
    /// 3. Group by status (Success, Failed, etc.).
    /// 4. Calculate average execution time.
    /// 5. Calculate total rows returned.
    /// 6. Identify most queried tables.
    /// 7. Return aggregated statistics.
    /// </remarks>
    Task<QueryStatistics> GetUserQueryStatisticsAsync(
        string userId,
        Guid? connectionId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exports query history to CSV format.
    /// </summary>
    /// <param name="userId">User identifier to export history for.</param>
    /// <param name="connectionId">Optional connection filter.</param>
    /// <param name="dateFrom">Optional start date filter.</param>
    /// <param name="dateTo">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token for operation.</param>
    /// <returns>CSV byte array with query history data.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query QueryHistory with filters (no pagination - all matching records).
    /// 2. Map to CSV-friendly format.
    /// 3. Generate CSV with headers: Timestamp, Prompt, SQL, Status, Rows, Time.
    /// 4. Return byte array for file download.
    /// </remarks>
    Task<byte[]> ExportQueryHistoryToCsvAsync(
        string userId,
        Guid? connectionId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches query history by keyword in prompts or SQL.
    /// </summary>
    /// <param name="userId">User identifier to search history for.</param>
    /// <param name="searchTerm">Keyword to search in UserPrompt and GeneratedSql fields.</param>
    /// <param name="connectionId">Optional connection filter.</param>
    /// <param name="pageNumber">Page number for pagination.</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Paginated search results.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query QueryHistory by user id.
    /// 2. Apply full-text search on UserPrompt and GeneratedSql.
    /// 3. Apply connection filter if provided.
    /// 4. Order by relevance or ExecutedAt descending.
    /// 5. Apply pagination.
    /// 6. Return search results.
    /// </remarks>
    Task<PaginatedQueryHistory> SearchQueryHistoryAsync(
        string userId,
        string searchTerm,
        Guid? connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
