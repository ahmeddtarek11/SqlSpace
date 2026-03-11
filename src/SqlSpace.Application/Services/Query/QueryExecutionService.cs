using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.AI;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Query;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.DTOs.AI;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.Query;

public class QueryExecutionService(
    ILogger<QueryExecutionService> logger,
    IApplicationDbContext dbcontext,
    IAccessControlService accessControlService,
    ISchemaContextService schemaContextService,
    ITextToSqlClient textToSqlClient,
    ISqlValidator sqlValidator,
    IDatabaseExecutor databaseExecutor,
    IQueryHistoryService queryHistoryService) : IQueryExecutionService
{
    private readonly ILogger<QueryExecutionService> _logger = logger;
    private readonly IApplicationDbContext _dbcontext = dbcontext;
    private readonly IAccessControlService _accessControlService = accessControlService;
    private readonly ISchemaContextService _schemaContextService = schemaContextService;
    private readonly ITextToSqlClient _textToSqlClient = textToSqlClient;
    private readonly ISqlValidator _sqlValidator = sqlValidator;
    private readonly IDatabaseExecutor _databaseExecutor = databaseExecutor;
    private readonly IQueryHistoryService _queryHistoryService = queryHistoryService;

    public async Task<Result<QueryExecutionResult>> ExecutePromptAsync(
        Guid connectionId,
        string userId,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.invalid_connection_id", "ConnectionId cannot be empty.", nameof(connectionId)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.invalid_prompt", "UserPrompt is required.", nameof(userPrompt)));
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
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.connection_not_found", "Connection not found.", nameof(connectionId)));
            }

            var isAdmin = string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal);

            var accessResult = await _accessControlService.HasAccessToConnectionAsync(
                connectionId,
                userId,
                cancellationToken);

            if (accessResult.IsFailure)
            {
                return Result<QueryExecutionResult>.Failure(accessResult.Errors);
            }

            if (accessResult.Value != true)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.forbidden", "User does not have access to this connection.", nameof(userId)));
            }

            var accessibleResult = await _accessControlService.GetAccessibleTableNamesAsync(
                connectionId,
                userId,
                cancellationToken);

            if (accessibleResult.IsFailure)
            {
                return Result<QueryExecutionResult>.Failure(accessibleResult.Errors);
            }

            var accessibleTables = accessibleResult.Value?.ToList() ?? new List<string>();
            if (accessibleTables.Count == 0)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.no_accessible_tables", "No accessible tables found for the user."));
            }

            var schemaContext = await _schemaContextService.GetFilteredSchemaForPromptAsync(
                connectionId,
                userId,
                userProvidedSchemaOverride: null,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(schemaContext))
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.schema_unavailable", "Schema context is unavailable for this connection."));
            }

            var restrictedSnapshot = await LoadRestrictedTablesSnapshotAsync(
                connectionId,
                userId,
                isAdmin,
                cancellationToken);

            var llmRequest = new SqlGenerationRequest
            {
                UserPrompt = userPrompt,
                SchemaContext = schemaContext,
                AccessibleTables = accessibleTables,
                DatabaseProvider = connection.DatabaseProvider.ToString()
            };

            var llmResult = await _textToSqlClient.SendSqlGenerationRequestAsync(llmRequest, cancellationToken);
            if (llmResult.IsFailure)
            {
                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    userPrompt,
                    generatedSql: string.Empty,
                    llmResponse: null,
                    status: QueryStatus.LlmError,
                    errorMessage: llmResult.Errors.FirstOrDefault()?.Message,
                    dbResult: null,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var generatedSql = llmResult.Value?.GeneratedSql ?? string.Empty;
            if (string.IsNullOrWhiteSpace(generatedSql))
            {
                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    userPrompt,
                    generatedSql: string.Empty,
                    llmResponse: llmResult.Value?.Explanation,
                    status: QueryStatus.LlmError,
                    errorMessage: "LLM returned empty SQL.",
                    dbResult: null,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var validation = await _sqlValidator.ValidateQueryAsync(
                generatedSql,
                accessibleTables,
                cancellationToken);

            if (!validation.IsValid)
            {
                var status = validation.UnauthorizedTables.Count > 0
                    ? QueryStatus.InsufficientPermissions
                    : QueryStatus.ValidationFailed;

                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    userPrompt,
                    generatedSql,
                    llmResult.Value?.Explanation,
                    status,
                    validation.ErrorMessage ?? "SQL validation failed.",
                    dbResult: null,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var dbResult = await _databaseExecutor.ExecuteQueryAsync(connection, generatedSql, cancellationToken);
            if (!dbResult.Success)
            {
                var status = dbResult.ErrorMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true
                    ? QueryStatus.Timeout
                    : QueryStatus.ExecutionFailed;

                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    userPrompt,
                    generatedSql,
                    llmResult.Value?.Explanation,
                    status,
                    dbResult.ErrorMessage ?? "Query execution failed.",
                    dbResult,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var successHistory = await SaveHistoryAsync(
                connection,
                userId,
                userPrompt,
                generatedSql,
                llmResult.Value?.Explanation,
                QueryStatus.Success,
                errorMessage: null,
                dbResult,
                accessibleTables,
                restrictedSnapshot,
                wasAdmin: isAdmin,
                cancellationToken);

            return BuildResult(successHistory);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Query execution cancelled for connection {ConnectionId} and user {UserId}",
                connectionId,
                userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Query execution failed for connection {ConnectionId} and user {UserId}",
                connectionId,
                userId);

            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.unexpected", "Unexpected error while executing query."));
        }
    }

    public Task<Result<PaginatedQueryHistory>> GetUserQueryHistoryAsync(
        string userId,
        Guid? connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return _queryHistoryService.GetUserQueryHistoryAsync(
            userId,
            connectionId,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public async Task<Result<QueryExecutionResult>> RerunQueryAsync(
        Guid queryId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (queryId == Guid.Empty)
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.invalid_query_id", "QueryId cannot be empty.", nameof(queryId)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        try
        {
            var original = await _dbcontext.QueryHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QueryId == queryId, cancellationToken);

            if (original is null)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.query_not_found", "Query history record not found.", nameof(queryId)));
            }

            var connection = await _dbcontext.ConnectedDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == original.DatabaseConnectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.connection_not_found", "Connection not found.", nameof(original.DatabaseConnectionId)));
            }

            var isAdmin = string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal);
            var isOwner = string.Equals(original.UserId, userId, StringComparison.Ordinal);

            if (!isAdmin && !isOwner)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.forbidden", "User is not authorized to rerun this query.", nameof(userId)));
            }

            var accessResult = await _accessControlService.HasAccessToConnectionAsync(
                connection.ConnectionId,
                userId,
                cancellationToken);

            if (accessResult.IsFailure)
            {
                return Result<QueryExecutionResult>.Failure(accessResult.Errors);
            }

            if (accessResult.Value != true)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.forbidden", "User does not have access to this connection.", nameof(userId)));
            }

            var accessibleResult = await _accessControlService.GetAccessibleTableNamesAsync(
                connection.ConnectionId,
                userId,
                cancellationToken);

            if (accessibleResult.IsFailure)
            {
                return Result<QueryExecutionResult>.Failure(accessibleResult.Errors);
            }

            var accessibleTables = accessibleResult.Value?.ToList() ?? new List<string>();
            if (accessibleTables.Count == 0)
            {
                return Result<QueryExecutionResult>.Failure(
                    new Error("query_execution.no_accessible_tables", "No accessible tables found for the user."));
            }

            var restrictedSnapshot = await LoadRestrictedTablesSnapshotAsync(
                connection.ConnectionId,
                userId,
                isAdmin,
                cancellationToken);

            var sql = original.GeneratedSql ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sql))
            {
                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    original.UserPrompt,
                    generatedSql: string.Empty,
                    llmResponse: original.LlmResponse,
                    status: QueryStatus.ValidationFailed,
                    errorMessage: "Original query has no SQL to rerun.",
                    dbResult: null,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var validation = await _sqlValidator.ValidateQueryAsync(sql, accessibleTables, cancellationToken);
            if (!validation.IsValid)
            {
                var status = validation.UnauthorizedTables.Count > 0
                    ? QueryStatus.InsufficientPermissions
                    : QueryStatus.ValidationFailed;

                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    original.UserPrompt,
                    sql,
                    original.LlmResponse,
                    status,
                    validation.ErrorMessage ?? "SQL validation failed.",
                    dbResult: null,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var dbResult = await _databaseExecutor.ExecuteQueryAsync(connection, sql, cancellationToken);
            if (!dbResult.Success)
            {
                var status = dbResult.ErrorMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true
                    ? QueryStatus.Timeout
                    : QueryStatus.ExecutionFailed;

                var history = await SaveHistoryAsync(
                    connection,
                    userId,
                    original.UserPrompt,
                    sql,
                    original.LlmResponse,
                    status,
                    dbResult.ErrorMessage ?? "Query execution failed.",
                    dbResult,
                    accessibleTables,
                    restrictedSnapshot,
                    wasAdmin: isAdmin,
                    cancellationToken);

                return BuildResult(history);
            }

            var successHistory = await SaveHistoryAsync(
                connection,
                userId,
                original.UserPrompt,
                sql,
                original.LlmResponse,
                QueryStatus.Success,
                errorMessage: null,
                dbResult,
                accessibleTables,
                restrictedSnapshot,
                wasAdmin: isAdmin,
                cancellationToken);

            return BuildResult(successHistory);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Query rerun cancelled for query {QueryId} and user {UserId}",
                queryId,
                userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Query rerun failed for query {QueryId} and user {UserId}",
                queryId,
                userId);

            return Result<QueryExecutionResult>.Failure(
                new Error("query_execution.unexpected", "Unexpected error while rerunning query."));
        }
    }

    private async Task<QueryHistory> SaveHistoryAsync(
        ConnectedDatabase connection,
        string userId,
        string userPrompt,
        string generatedSql,
        string? llmResponse,
        QueryStatus status,
        string? errorMessage,
        DatabaseQueryResult? dbResult,
        IReadOnlyList<string> accessibleTables,
        string? restrictedTablesSnapshot,
        bool wasAdmin,
        CancellationToken cancellationToken)
    {
        var history = new QueryHistory
        {
            QueryId = Guid.NewGuid(),
            UserId = userId,
            DatabaseConnectionId = connection.ConnectionId,
            UserPrompt = userPrompt,
            GeneratedSql = generatedSql,
            LlmResponse = llmResponse,
            Status = status,
            ErrorMessage = errorMessage,
            ResultsJson = dbResult?.ResultsJson,
            RowsReturned = dbResult?.RowsReturned,
            ExecutionTimeMs = dbResult?.ExecutionTimeMs,
            ExecutedAt = DateTime.UtcNow,
            AccessibleTablesSnapshot = SerializeSnapshot(accessibleTables),
            RestrictedTablesSnapshot = restrictedTablesSnapshot,
            WasAdminAtExecution = wasAdmin
        };

        await _dbcontext.QueryHistories.AddAsync(history, cancellationToken);
        await _dbcontext.SaveChangesAsync(cancellationToken);

        return history;
    }

    private async Task<string?> LoadRestrictedTablesSnapshotAsync(
        Guid connectionId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (isAdmin)
        {
            return null;
        }

        var access = await _dbcontext.UserDatabaseAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ua => ua.DatabaseConnectionId == connectionId
                    && ua.UserId == userId
                    && !ua.IsDeleted
                    && ua.RevokedAt == null,
                cancellationToken);

        if (access is null || access.HasFullAccess)
        {
            return null;
        }

        return access.RestrictedTablesJson;
    }

    private static string? SerializeSnapshot(IReadOnlyList<string> tables)
    {
        return JsonSerializer.Serialize(tables);
    }

    private static Result<QueryExecutionResult> BuildResult(QueryHistory history)
    {
        var result = new QueryExecutionResult
        {
            Success = history.Status == QueryStatus.Success,
            QueryHistoryId = history.QueryId,
            GeneratedSql = history.GeneratedSql,
            LlmExplanation = history.LlmResponse,
            ResultsJson = history.ResultsJson,
            RowsReturned = history.RowsReturned,
            ExecutionTimeMs = history.ExecutionTimeMs ?? 0,
            Status = history.Status,
            ErrorMessage = history.ErrorMessage
        };

        return result;
    }
}
