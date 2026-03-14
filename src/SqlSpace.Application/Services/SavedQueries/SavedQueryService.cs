using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Query;
using SqlSpace.Application.Abstractions.SavedQueries;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.SavedQueries;

public sealed class SavedQueryService(
    IApplicationDbContext dbContext,
    IAccessControlService accessControlService,
    IQueryExecutionService queryExecutionService,
    ILogger<SavedQueryService> logger) : ISavedQueryService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IAccessControlService _accessControlService = accessControlService;
    private readonly IQueryExecutionService _queryExecutionService = queryExecutionService;
    private readonly ILogger<SavedQueryService> _logger = logger;

    public async Task<Result<IReadOnlyList<SavedQueryDto>>> GetSavedQueriesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<IReadOnlyList<SavedQueryDto>>.Failure(
                new Error("saved_queries.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        var queries = await _dbContext.SavedQueries
            .AsNoTracking()
            .Include(q => q.DatabaseConnection)
            .Where(q => q.UserId == userId && !q.DatabaseConnection.IsDeleted)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Select(q => new SavedQueryDto
            {
                Id = q.Id,
                Name = q.Name,
                UserPrompt = q.UserPrompt,
                GeneratedSql = q.GeneratedSql,
                ConnectionId = q.DatabaseConnectionId,
                ConnectionName = q.DatabaseConnection.ConnectionName,
                QueryHistoryId = q.QueryHistoryId,
                CreatedAtUtc = q.CreatedAtUtc,
                UpdatedAtUtc = q.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return queries;
    }

    public async Task<Result<SavedQueryDto>> CreateSavedQueryAsync(
        CreateSavedQueryRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_request", "Request is required.", nameof(request)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_user_id", "UserId is required.", nameof(userId)));
        }


        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_name", "Name is required.", nameof(request.Name)));
        }

        if (request.QueryHistoryId == Guid.Empty)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_history_id", "QueryHistoryId is required.", nameof(request.QueryHistoryId)));
        }

        var history = await _dbContext.QueryHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.QueryId == request.QueryHistoryId && h.UserId == userId, cancellationToken);

        if (history is null)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.history_not_found", "Query history record not found for the user.",
                    nameof(request.QueryHistoryId)));
        }

        if (string.IsNullOrWhiteSpace(history.GeneratedSql))
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_sql", "GeneratedSql is missing for the history record.",
                    nameof(request.QueryHistoryId)));
        }

        var accessResult = await _accessControlService.HasAccessToConnectionAsync(
            history.DatabaseConnectionId,
            userId,
            cancellationToken);

        if (accessResult.IsFailure)
        {
            return Result<SavedQueryDto>.Failure(accessResult.Errors);
        }

        if (accessResult.Value != true)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.forbidden", "User does not have access to this connection.", nameof(userId)));
        }

        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == history.DatabaseConnectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.connection_not_found", "Connection not found.", nameof(history.DatabaseConnectionId)));
        }

        var now = DateTime.UtcNow;
        var savedQuery = new SavedQuery
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DatabaseConnectionId = history.DatabaseConnectionId,
            Name = request.Name.Trim(),
            UserPrompt = history.UserPrompt?.Trim() ?? string.Empty,
            GeneratedSql = history.GeneratedSql.Trim(),
            QueryHistoryId = history.QueryId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _dbContext.SavedQueries.AddAsync(savedQuery, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SavedQueryDto
        {
            Id = savedQuery.Id,
            Name = savedQuery.Name,
            UserPrompt = savedQuery.UserPrompt,
            GeneratedSql = savedQuery.GeneratedSql,
            ConnectionId = savedQuery.DatabaseConnectionId,
            ConnectionName = connection.ConnectionName,
            QueryHistoryId = savedQuery.QueryHistoryId,
            CreatedAtUtc = savedQuery.CreatedAtUtc,
            UpdatedAtUtc = savedQuery.UpdatedAtUtc
        };
    }

    public async Task<Result<SavedQueryDto>> RenameSavedQueryAsync(
        Guid savedQueryId,
        string name,
        string userId,
        CancellationToken cancellationToken)
    {
        if (savedQueryId == Guid.Empty)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_id", "Saved query id is required.", nameof(savedQueryId)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.invalid_name", "Name is required.", nameof(name)));
        }

        var savedQuery = await _dbContext.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == savedQueryId && q.UserId == userId, cancellationToken);

        if (savedQuery is null)
        {
            return Result<SavedQueryDto>.Failure(
                new Error("saved_queries.not_found", "Saved query not found.", nameof(savedQueryId)));
        }

        savedQuery.Name = name.Trim();
        savedQuery.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var connectionName = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .Where(c => c.ConnectionId == savedQuery.DatabaseConnectionId)
            .Select(c => c.ConnectionName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new SavedQueryDto
        {
            Id = savedQuery.Id,
            Name = savedQuery.Name,
            UserPrompt = savedQuery.UserPrompt,
            GeneratedSql = savedQuery.GeneratedSql,
            ConnectionId = savedQuery.DatabaseConnectionId,
            ConnectionName = connectionName,
            QueryHistoryId = savedQuery.QueryHistoryId,
            CreatedAtUtc = savedQuery.CreatedAtUtc,
            UpdatedAtUtc = savedQuery.UpdatedAtUtc
        };
    }

    public async Task<Result<bool>> DeleteSavedQueryAsync(
        Guid savedQueryId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (savedQueryId == Guid.Empty)
        {
            return Result<bool>.Failure(
                new Error("saved_queries.invalid_id", "Saved query id is required.", nameof(savedQueryId)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<bool>.Failure(
                new Error("saved_queries.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        var savedQuery = await _dbContext.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == savedQueryId && q.UserId == userId, cancellationToken);

        if (savedQuery is null)
        {
            return Result<bool>.Failure(
                new Error("saved_queries.not_found", "Saved query not found.", nameof(savedQueryId)));
        }

        _dbContext.SavedQueries.Remove(savedQuery);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<Result<QueryExecutionResult>> ExecuteSavedQueryAsync(
        Guid savedQueryId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (savedQueryId == Guid.Empty)
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("saved_queries.invalid_id", "Saved query id is required.", nameof(savedQueryId)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("saved_queries.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        var savedQuery = await _dbContext.SavedQueries
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == savedQueryId && q.UserId == userId, cancellationToken);

        if (savedQuery is null)
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("saved_queries.not_found", "Saved query not found.", nameof(savedQueryId)));
        }

        try
        {
            return await _queryExecutionService.ExecuteSqlAsync(
                savedQuery.DatabaseConnectionId,
                userId,
                savedQuery.UserPrompt,
                savedQuery.GeneratedSql,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute saved query {SavedQueryId} for user {UserId}.", savedQueryId, userId);
            return Result<QueryExecutionResult>.Failure(
                new Error("saved_queries.execute_failed", "Failed to execute saved query.", nameof(savedQueryId)));
        }
    }
}
