using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Analytics;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.DTOs.Analytics;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.Analytics;

public sealed class ChartService(
    IApplicationDbContext dbContext,
    IAccessControlService accessControlService,
    ISchemaContextService schemaContextService,
    IAnalyticsAiClient analyticsAiClient,
    IDatabaseExecutor databaseExecutor,
    ILogger<ChartService> logger) : IChartService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IAccessControlService _accessControlService = accessControlService;
    private readonly ISchemaContextService _schemaContextService = schemaContextService;
    private readonly IAnalyticsAiClient _analyticsAiClient = analyticsAiClient;
    private readonly IDatabaseExecutor _databaseExecutor = databaseExecutor;
    private readonly ILogger<ChartService> _logger = logger;

    public async Task<Result<IReadOnlyList<ChartSuggestionDto>>> SuggestChartsAsync(
        Guid connectionId,
        string userId,
        string? userPrompt,
        int maxSuggestions,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics.invalid_connection_id", "ConnectionId is required."));

        if (string.IsNullOrWhiteSpace(userId))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics.invalid_user_id", "UserId is required."));

        var accessResult = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (accessResult.IsFailure)
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(accessResult.Errors);
        if (accessResult.Value != true)
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics.forbidden", "User does not have access to this connection."));

        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics.connection_not_found", "Connection not found."));

        var schemaContext = await _schemaContextService.GetFilteredSchemaForPromptAsync(
            connectionId, userId, null, cancellationToken);

        if (string.IsNullOrWhiteSpace(schemaContext))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics.empty_schema", "No schema available for this connection."));

        var providerName = connection.DatabaseProvider.ToString();

        var result = await _analyticsAiClient.GetChartSuggestionsAsync(
            schemaContext, providerName, userPrompt, null, maxSuggestions, cancellationToken);

        return result;
    }

    public async Task<Result<IReadOnlyList<SavedChartDto>>> GetChartsForConnectionAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
            return Result<IReadOnlyList<SavedChartDto>>.Failure(
                new Error("analytics.invalid_connection_id", "ConnectionId is required."));

        if (string.IsNullOrWhiteSpace(userId))
            return Result<IReadOnlyList<SavedChartDto>>.Failure(
                new Error("analytics.invalid_user_id", "UserId is required."));

        var charts = await _dbContext.SavedCharts
            .AsNoTracking()
            .Include(c => c.DatabaseConnection)
            .Where(c => c.DatabaseConnectionId == connectionId
                        && c.UserId == userId
                        && !c.IsDeleted
                        && !c.DatabaseConnection.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CreatedAtUtc)
            .Select(c => new SavedChartDto
            {
                Id = c.Id,
                ConnectionId = c.DatabaseConnectionId,
                ConnectionName = c.DatabaseConnection.ConnectionName,
                Title = c.Title,
                Description = c.Description,
                SqlQuery = c.SqlQuery,
                OriginalPrompt = c.OriginalPrompt,
                ChartType = c.ChartType.ToString().ToLowerInvariant(),
                ChartConfigJson = c.ChartConfigJson,
                GridX = c.GridX,
                GridY = c.GridY,
                GridW = c.GridW,
                GridH = c.GridH,
                SortOrder = c.SortOrder,
                CreatedAtUtc = c.CreatedAtUtc,
                UpdatedAtUtc = c.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return charts;
    }

    public async Task<Result<SavedChartDto>> SaveChartAsync(
        SaveChartRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Result<SavedChartDto>.Failure(
                new Error("analytics.invalid_request", "Request is required."));

        if (string.IsNullOrWhiteSpace(userId))
            return Result<SavedChartDto>.Failure(
                new Error("analytics.invalid_user_id", "UserId is required."));

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<SavedChartDto>.Failure(
                new Error("analytics.invalid_title", "Title is required."));

        if (string.IsNullOrWhiteSpace(request.SqlQuery))
            return Result<SavedChartDto>.Failure(
                new Error("analytics.invalid_sql", "SQL query is required."));

        var accessResult = await _accessControlService.HasAccessToConnectionAsync(
            request.ConnectionId, userId, cancellationToken);
        if (accessResult.IsFailure)
            return Result<SavedChartDto>.Failure(accessResult.Errors);
        if (accessResult.Value != true)
            return Result<SavedChartDto>.Failure(
                new Error("analytics.forbidden", "User does not have access to this connection."));

        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == request.ConnectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
            return Result<SavedChartDto>.Failure(
                new Error("analytics.connection_not_found", "Connection not found."));

        if (!Enum.TryParse<ChartType>(request.ChartType, true, out var chartType))
            chartType = ChartType.Bar;

        var now = DateTime.UtcNow;
        var chart = new SavedChart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DatabaseConnectionId = request.ConnectionId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            SqlQuery = request.SqlQuery.Trim(),
            OriginalPrompt = request.OriginalPrompt?.Trim(),
            ChartType = chartType,
            ChartConfigJson = request.ChartConfigJson ?? "{}",
            GridX = request.GridX,
            GridY = request.GridY,
            GridW = request.GridW > 0 ? request.GridW : 6,
            GridH = request.GridH > 0 ? request.GridH : 4,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await _dbContext.SavedCharts.AddAsync(chart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Chart saved. ChartId: {ChartId}, UserId: {UserId}", chart.Id, userId);

        return new SavedChartDto
        {
            Id = chart.Id,
            ConnectionId = chart.DatabaseConnectionId,
            ConnectionName = connection.ConnectionName,
            Title = chart.Title,
            Description = chart.Description,
            SqlQuery = chart.SqlQuery,
            OriginalPrompt = chart.OriginalPrompt,
            ChartType = chart.ChartType.ToString().ToLowerInvariant(),
            ChartConfigJson = chart.ChartConfigJson,
            GridX = chart.GridX,
            GridY = chart.GridY,
            GridW = chart.GridW,
            GridH = chart.GridH,
            SortOrder = chart.SortOrder,
            CreatedAtUtc = chart.CreatedAtUtc,
            UpdatedAtUtc = chart.UpdatedAtUtc,
        };
    }

    public async Task<Result<SavedChartDto>> UpdateChartAsync(
        Guid chartId,
        UpdateChartRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        if (chartId == Guid.Empty)
            return Result<SavedChartDto>.Failure(
                new Error("analytics.invalid_chart_id", "ChartId is required."));

        var chart = await _dbContext.SavedCharts
            .Include(c => c.DatabaseConnection)
            .FirstOrDefaultAsync(c => c.Id == chartId && c.UserId == userId && !c.IsDeleted, cancellationToken);

        if (chart is null)
            return Result<SavedChartDto>.Failure(
                new Error("analytics.chart_not_found", "Chart not found."));

        if (!string.IsNullOrWhiteSpace(request.Title))
            chart.Title = request.Title.Trim();
        if (request.Description is not null)
            chart.Description = request.Description.Trim();
        if (!string.IsNullOrWhiteSpace(request.SqlQuery))
            chart.SqlQuery = request.SqlQuery.Trim();
        if (!string.IsNullOrWhiteSpace(request.ChartType) &&
            Enum.TryParse<ChartType>(request.ChartType, true, out var newChartType))
            chart.ChartType = newChartType;
        if (!string.IsNullOrWhiteSpace(request.ChartConfigJson))
            chart.ChartConfigJson = request.ChartConfigJson;

        chart.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SavedChartDto
        {
            Id = chart.Id,
            ConnectionId = chart.DatabaseConnectionId,
            ConnectionName = chart.DatabaseConnection.ConnectionName,
            Title = chart.Title,
            Description = chart.Description,
            SqlQuery = chart.SqlQuery,
            OriginalPrompt = chart.OriginalPrompt,
            ChartType = chart.ChartType.ToString().ToLowerInvariant(),
            ChartConfigJson = chart.ChartConfigJson,
            GridX = chart.GridX,
            GridY = chart.GridY,
            GridW = chart.GridW,
            GridH = chart.GridH,
            SortOrder = chart.SortOrder,
            CreatedAtUtc = chart.CreatedAtUtc,
            UpdatedAtUtc = chart.UpdatedAtUtc,
        };
    }

    public async Task<Result<bool>> DeleteChartAsync(
        Guid chartId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (chartId == Guid.Empty)
            return Result<bool>.Failure(
                new Error("analytics.invalid_chart_id", "ChartId is required."));

        var chart = await _dbContext.SavedCharts
            .FirstOrDefaultAsync(c => c.Id == chartId && c.UserId == userId && !c.IsDeleted, cancellationToken);

        if (chart is null)
            return Result<bool>.Failure(
                new Error("analytics.chart_not_found", "Chart not found."));

        chart.IsDeleted = true;
        chart.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Chart deleted. ChartId: {ChartId}, UserId: {UserId}", chartId, userId);
        return true;
    }

    public async Task<Result<ChartDataResult>> ExecuteChartAsync(
        Guid chartId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (chartId == Guid.Empty)
            return Result<ChartDataResult>.Failure(
                new Error("analytics.invalid_chart_id", "ChartId is required."));

        var chart = await _dbContext.SavedCharts
            .AsNoTracking()
            .Include(c => c.DatabaseConnection)
            .FirstOrDefaultAsync(c => c.Id == chartId && c.UserId == userId && !c.IsDeleted, cancellationToken);

        if (chart is null)
            return Result<ChartDataResult>.Failure(
                new Error("analytics.chart_not_found", "Chart not found."));

        if (chart.DatabaseConnection.IsDeleted)
            return Result<ChartDataResult>.Failure(
                new Error("analytics.connection_deleted", "The database connection has been deleted."));

        var accessResult = await _accessControlService.HasAccessToConnectionAsync(
            chart.DatabaseConnectionId, userId, cancellationToken);
        if (accessResult.IsFailure || accessResult.Value != true)
            return new ChartDataResult
            {
                ChartId = chartId,
                Success = false,
                ErrorMessage = "Insufficient permissions to execute this chart."
            };

        try
        {
            var queryResult = await _databaseExecutor.ExecuteQueryAsync(
                chart.DatabaseConnection, chart.SqlQuery, cancellationToken);

            return new ChartDataResult
            {
                ChartId = chartId,
                Success = queryResult.Success,
                ResultsJson = queryResult.ResultsJson,
                RowsReturned = queryResult.RowsReturned,
                ExecutionTimeMs = queryResult.ExecutionTimeMs,
                ErrorMessage = queryResult.ErrorMessage,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute chart {ChartId}.", chartId);
            return new ChartDataResult
            {
                ChartId = chartId,
                Success = false,
                ErrorMessage = $"Execution failed: {ex.GetType().Name}"
            };
        }
    }

    public async Task<Result<IReadOnlyList<ChartDataResult>>> RefreshAllChartsAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
            return Result<IReadOnlyList<ChartDataResult>>.Failure(
                new Error("analytics.invalid_connection_id", "ConnectionId is required."));

        var charts = await _dbContext.SavedCharts
            .AsNoTracking()
            .Include(c => c.DatabaseConnection)
            .Where(c => c.DatabaseConnectionId == connectionId
                        && c.UserId == userId
                        && !c.IsDeleted
                        && !c.DatabaseConnection.IsDeleted)
            .ToListAsync(cancellationToken);

        if (charts.Count == 0)
            return Result<IReadOnlyList<ChartDataResult>>.Success(Array.Empty<ChartDataResult>());

        var semaphore = new SemaphoreSlim(10);
        var tasks = charts.Select(async chart =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var queryResult = await _databaseExecutor.ExecuteQueryAsync(
                    chart.DatabaseConnection, chart.SqlQuery, cancellationToken);

                return new ChartDataResult
                {
                    ChartId = chart.Id,
                    Success = queryResult.Success,
                    ResultsJson = queryResult.ResultsJson,
                    RowsReturned = queryResult.RowsReturned,
                    ExecutionTimeMs = queryResult.ExecutionTimeMs,
                    ErrorMessage = queryResult.ErrorMessage,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh chart {ChartId}.", chart.Id);
                return new ChartDataResult
                {
                    ChartId = chart.Id,
                    Success = false,
                    ErrorMessage = $"Execution failed: {ex.GetType().Name}"
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<Result<bool>> UpdateLayoutAsync(
        Guid connectionId,
        string userId,
        IReadOnlyList<ChartLayoutUpdate> layouts,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
            return Result<bool>.Failure(
                new Error("analytics.invalid_connection_id", "ConnectionId is required."));

        if (layouts is null || layouts.Count == 0)
            return Result<bool>.Failure(
                new Error("analytics.invalid_layouts", "Layout updates are required."));

        var chartIds = layouts.Select(l => l.ChartId).ToHashSet();

        var charts = await _dbContext.SavedCharts
            .Where(c => chartIds.Contains(c.Id)
                        && c.DatabaseConnectionId == connectionId
                        && c.UserId == userId
                        && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var chartMap = charts.ToDictionary(c => c.Id);

        foreach (var layout in layouts)
        {
            if (chartMap.TryGetValue(layout.ChartId, out var chart))
            {
                chart.GridX = layout.GridX;
                chart.GridY = layout.GridY;
                chart.GridW = layout.GridW;
                chart.GridH = layout.GridH;
                chart.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
