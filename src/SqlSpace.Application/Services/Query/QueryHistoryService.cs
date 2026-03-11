using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Query;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Services.Query;

public class QueryHistoryService(
    ILogger<QueryHistoryService> logger,
    IApplicationDbContext dbcontext) : IQueryHistoryService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ILogger<QueryHistoryService> _logger = logger;
    private readonly IApplicationDbContext _dbcontext = dbcontext;

    public async Task<Result<PaginatedQueryHistory>> GetConnectionQueryHistoryAsync(
        Guid connectionId,
        string requestingUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.invalid_connection_id", "ConnectionId cannot be empty.", nameof(connectionId)));
        }

        if (string.IsNullOrWhiteSpace(requestingUserId))
        {
            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.invalid_user_id", "Requesting user id is required.", nameof(requestingUserId)));
        }

        try
        {
            var connection = await _dbcontext.ConnectedDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == connectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                return Result<PaginatedQueryHistory>.Failure(
                    new Error("query_history.connection_not_found", "Connection not found.", nameof(connectionId)));
            }

            if (!string.Equals(connection.DbAdminId, requestingUserId, StringComparison.Ordinal))
            {
                return Result<PaginatedQueryHistory>.Failure(
                    new Error("query_history.forbidden", "User is not authorized to view this connection history.", nameof(requestingUserId)));
            }

            var (normalizedPageNumber, normalizedPageSize, skip) = NormalizePagination(pageNumber, pageSize);

            var query = _dbcontext.QueryHistories
                .AsNoTracking()
                .Where(q => q.DatabaseConnectionId == connectionId)
                .OrderByDescending(q => q.ExecutedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip(skip)
                .Take(normalizedPageSize)
                .Select(q => new QueryHistoryDto
                {
                    QueryId = q.QueryId,
                    UserPrompt = q.UserPrompt,
                    GeneratedSql = q.GeneratedSql,
                    Status = q.Status,
                    RowsReturned = q.RowsReturned,
                    ExecutionTimeMs = q.ExecutionTimeMs,
                    ExecutedAt = q.ExecutedAt,
                    ConnectionName = q.DatabaseConnection.ConnectionName
                })
                .ToListAsync(cancellationToken);

            return new PaginatedQueryHistory
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load connection query history. ConnectionId: {ConnectionId}, UserId: {UserId}",
                connectionId,
                requestingUserId);

            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.query_failed", "Failed to load query history."));
        }
    }

    public async Task<Result<QueryHistoryDetailDto?>> GetQueryByIdAsync(
        Guid queryId,
        string requestingUserId,
        CancellationToken cancellationToken)
    {
        if (queryId == Guid.Empty)
        {
            return Result<QueryHistoryDetailDto?>.Failure(
                new Error("query_history.invalid_query_id", "QueryId cannot be empty.", nameof(queryId)));
        }

        if (string.IsNullOrWhiteSpace(requestingUserId))
        {
            return Result<QueryHistoryDetailDto?>.Failure(
                new Error("query_history.invalid_user_id", "Requesting user id is required.", nameof(requestingUserId)));
        }

        try
        {
            var query = await _dbcontext.QueryHistories
                .AsNoTracking()
                .Include(q => q.DatabaseConnection)
                .FirstOrDefaultAsync(q => q.QueryId == queryId, cancellationToken);

            if (query is null)
            {
                return Result<QueryHistoryDetailDto?>.Failure(
                    new Error("query_history.not_found", "Query history record not found.", nameof(queryId)));
            }

            var isOwner = string.Equals(query.UserId, requestingUserId, StringComparison.Ordinal);
            var isAdmin = string.Equals(query.DatabaseConnection.DbAdminId, requestingUserId, StringComparison.Ordinal);

            if (!isOwner && !isAdmin)
            {
                return Result<QueryHistoryDetailDto?>.Failure(
                    new Error("query_history.forbidden", "User is not authorized to view this query.", nameof(requestingUserId)));
            }

            return new QueryHistoryDetailDto
            {
                QueryId = query.QueryId,
                UserId = query.UserId,
                UserEmail = string.Empty,
                ConnectionId = query.DatabaseConnectionId,
                ConnectionName = query.DatabaseConnection.ConnectionName,
                UserPrompt = query.UserPrompt,
                GeneratedSql = query.GeneratedSql,
                LlmResponse = query.LlmResponse,
                Status = query.Status,
                ErrorMessage = query.ErrorMessage,
                ResultsJson = query.ResultsJson,
                RowsReturned = query.RowsReturned,
                ExecutionTimeMs = query.ExecutionTimeMs,
                ExecutedAt = query.ExecutedAt,
                WasAdminAtExecution = query.WasAdminAtExecution
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load query history detail. QueryId: {QueryId}, UserId: {UserId}",
                queryId,
                requestingUserId);

            return Result<QueryHistoryDetailDto?>.Failure(
                new Error("query_history.query_failed", "Failed to load query history detail."));
        }
    }

    public async Task<Result<PaginatedQueryHistory>> GetUserQueryHistoryAsync(
        string userId,
        Guid? connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.invalid_user_id", "User id is required.", nameof(userId)));
        }

        if (connectionId.HasValue && connectionId.Value == Guid.Empty)
        {
            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.invalid_connection_id", "ConnectionId cannot be empty.", nameof(connectionId)));
        }

        try
        {
            var (normalizedPageNumber, normalizedPageSize, skip) = NormalizePagination(pageNumber, pageSize);

            var query = _dbcontext.QueryHistories
                .AsNoTracking()
                .Where(q => q.UserId == userId);

            if (connectionId.HasValue)
            {
                query = query.Where(q => q.DatabaseConnectionId == connectionId.Value);
            }

            query = query.OrderByDescending(q => q.ExecutedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip(skip)
                .Take(normalizedPageSize)
                .Select(q => new QueryHistoryDto
                {
                    QueryId = q.QueryId,
                    UserPrompt = q.UserPrompt,
                    GeneratedSql = q.GeneratedSql,
                    Status = q.Status,
                    RowsReturned = q.RowsReturned,
                    ExecutionTimeMs = q.ExecutionTimeMs,
                    ExecutedAt = q.ExecutedAt,
                    ConnectionName = q.DatabaseConnection.ConnectionName
                })
                .ToListAsync(cancellationToken);

            return new PaginatedQueryHistory
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load user query history. UserId: {UserId}",
                userId);

            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.query_failed", "Failed to load query history."));
        }
    }

    public async Task<Result<QueryStatistics>> GetUserQueryStatisticsAsync(
        string userId,
        Guid? connectionId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<QueryStatistics>.Failure(
                new Error("query_history.invalid_user_id", "User id is required.", nameof(userId)));
        }

        if (connectionId.HasValue && connectionId.Value == Guid.Empty)
        {
            return Result<QueryStatistics>.Failure(
                new Error("query_history.invalid_connection_id", "ConnectionId cannot be empty.", nameof(connectionId)));
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
        {
            return Result<QueryStatistics>.Failure(
                new Error("query_history.invalid_date_range", "dateFrom cannot be after dateTo.", nameof(dateFrom)));
        }

        try
        {
            var query = _dbcontext.QueryHistories
                .AsNoTracking()
                .Where(q => q.UserId == userId);

            if (connectionId.HasValue)
            {
                query = query.Where(q => q.DatabaseConnectionId == connectionId.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(q => q.ExecutedAt >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(q => q.ExecutedAt <= dateTo.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            if (totalCount == 0)
            {
                return new QueryStatistics
                {
                    TotalQueries = 0,
                    SuccessfulQueries = 0,
                    FailedQueries = 0,
                    AverageExecutionTimeMs = 0,
                    TotalRowsReturned = 0,
                    MostQueriedTables = Array.Empty<TableQueryCount>(),
                    FirstQueryDate = null,
                    LastQueryDate = null
                };
            }

            var successfulCount = await query.CountAsync(q => q.Status == QueryStatus.Success, cancellationToken);
            var averageExecutionTime = await query
                .Where(q => q.ExecutionTimeMs.HasValue)
                .AverageAsync(q => (double?)q.ExecutionTimeMs, cancellationToken) ?? 0d;

            var totalRows = await query
                .Where(q => q.RowsReturned.HasValue)
                .SumAsync(q => (long?)q.RowsReturned, cancellationToken) ?? 0L;

            var firstDate = await query.MinAsync(q => (DateTime?)q.ExecutedAt, cancellationToken);
            var lastDate = await query.MaxAsync(q => (DateTime?)q.ExecutedAt, cancellationToken);

            return new QueryStatistics
            {
                TotalQueries = totalCount,
                SuccessfulQueries = successfulCount,
                FailedQueries = totalCount - successfulCount,
                AverageExecutionTimeMs = averageExecutionTime,
                TotalRowsReturned = totalRows,
                MostQueriedTables = Array.Empty<TableQueryCount>(),
                FirstQueryDate = firstDate,
                LastQueryDate = lastDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load query statistics. UserId: {UserId}",
                userId);

            return Result<QueryStatistics>.Failure(
                new Error("query_history.query_failed", "Failed to load query statistics."));
        }
    }

    public async Task<Result<PaginatedQueryHistory>> SearchQueryHistoryAsync(
        string userId,
        string searchTerm,
        Guid? connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.invalid_user_id", "User id is required.", nameof(userId)));
        }

        if (connectionId.HasValue && connectionId.Value == Guid.Empty)
        {
            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.invalid_connection_id", "ConnectionId cannot be empty.", nameof(connectionId)));
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetUserQueryHistoryAsync(userId, connectionId, pageNumber, pageSize, cancellationToken);
        }

        try
        {
            var (normalizedPageNumber, normalizedPageSize, skip) = NormalizePagination(pageNumber, pageSize);
            var term = $"%{searchTerm.Trim()}%";

            var query = _dbcontext.QueryHistories
                .AsNoTracking()
                .Where(q => q.UserId == userId)
                .Where(q => EF.Functions.Like(q.UserPrompt, term) || EF.Functions.Like(q.GeneratedSql, term));

            if (connectionId.HasValue)
            {
                query = query.Where(q => q.DatabaseConnectionId == connectionId.Value);
            }

            query = query.OrderByDescending(q => q.ExecutedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip(skip)
                .Take(normalizedPageSize)
                .Select(q => new QueryHistoryDto
                {
                    QueryId = q.QueryId,
                    UserPrompt = q.UserPrompt,
                    GeneratedSql = q.GeneratedSql,
                    Status = q.Status,
                    RowsReturned = q.RowsReturned,
                    ExecutionTimeMs = q.ExecutionTimeMs,
                    ExecutedAt = q.ExecutedAt,
                    ConnectionName = q.DatabaseConnection.ConnectionName
                })
                .ToListAsync(cancellationToken);

            return new PaginatedQueryHistory
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to search query history. UserId: {UserId}, SearchTerm: {SearchTerm}",
                userId,
                searchTerm);

            return Result<PaginatedQueryHistory>.Failure(
                new Error("query_history.query_failed", "Failed to search query history."));
        }
    }

    private static (int PageNumber, int PageSize, int Skip) NormalizePagination(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;
        return (normalizedPageNumber, normalizedPageSize, skip);
    }
}
