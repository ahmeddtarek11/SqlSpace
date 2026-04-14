using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Analytics;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Reports;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.DTOs.Reports;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.Reports;

public sealed class ReportService(
    IApplicationDbContext dbContext,
    IAccessControlService accessControlService,
    ISchemaContextService schemaContextService,
    IReportAiClient reportAiClient,
    IDatabaseExecutor databaseExecutor,
    ILogger<ReportService> logger) : IReportService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IAccessControlService _accessControlService = accessControlService;
    private readonly ISchemaContextService _schemaContextService = schemaContextService;
    private readonly IReportAiClient _reportAiClient = reportAiClient;
    private readonly IDatabaseExecutor _databaseExecutor = databaseExecutor;
    private readonly ILogger<ReportService> _logger = logger;

    private const int MaxSections = 6;
    private const int MaxRowsForNarrative = 20;

    public async Task<Result<ReportDraftDto>> DraftAsync(
        Guid connectionId,
        string userId,
        string prompt,
        CancellationToken cancellationToken)
    {
        var access = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (access.IsFailure) return Result<ReportDraftDto>.Failure(access.Errors);
        if (!access.Value)
            return Result<ReportDraftDto>.Failure(new Error("reports.forbidden", "You do not have access to this connection."));

        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
            return Result<ReportDraftDto>.Failure(new Error("reports.connection_not_found", "Connection not found."));

        var schemaContext = await _schemaContextService.GetFilteredSchemaForPromptAsync(connectionId, userId, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(schemaContext))
            return Result<ReportDraftDto>.Failure(new Error("reports.empty_schema", "No schema available for this connection."));

        // 1. Ask LLM to plan sections
        var planResult = await _reportAiClient.PlanReportAsync(
            schemaContext, connection.DatabaseProvider.ToString(), prompt, MaxSections, cancellationToken);

        if (planResult.IsFailure)
            return Result<ReportDraftDto>.Failure(planResult.Errors);

        var plan = planResult.Value!;

        // 2. Materialize planned sections
        var sections = plan.Sections
            .Select((planned, i) => new ReportSectionDto
            {
                SectionId = Guid.NewGuid(),
                SortOrder = i,
                Heading = planned.Heading,
                ChartType = planned.ChartType,
                ChartConfigJson = planned.ChartConfig,
                SqlQuery = planned.Sql,
            })
            .ToList();

        // 3. Execute SQL for query-backed sections in parallel.
        var sqlExecutionTasks = sections
            .Where(s => !string.IsNullOrWhiteSpace(s.SqlQuery))
            .Select(async section =>
            {
                var queryResult = await _databaseExecutor.ExecuteQueryAsync(connection, section.SqlQuery!, cancellationToken);
                section.ExecutionSuccess = queryResult.Success;
                section.ResultsJson = queryResult.ResultsJson;
                section.RowsReturned = queryResult.RowsReturned;
                section.ExecutionTimeMs = queryResult.ExecutionTimeMs;
                section.ExecutionErrorMessage = queryResult.ErrorMessage;
                section.ExecutedAtUtc = DateTime.UtcNow;
            });

        await Task.WhenAll(sqlExecutionTasks);

        // 4. Narrate all sections with one AI round-trip.
        var narrateInputs = sections
            .OrderBy(s => s.SortOrder)
            .Select(s => new NarrateReportSectionInputDto
            {
                Heading = s.Heading,
                Sql = s.SqlQuery,
                ChartType = s.ChartType,
                SampleRowsJson = ExtractSampleRows(s.ResultsJson, MaxRowsForNarrative),
            })
            .ToList();

        var narrateResult = await _reportAiClient.NarrateReportAsync(
            plan.Title,
            prompt,
            plan.Summary,
            narrateInputs,
            cancellationToken);

        if (narrateResult.IsSuccess)
        {
            var narratives = narrateResult.Value ?? [];
            for (var i = 0; i < sections.Count; i++)
            {
                sections[i].NarrativeText = i < narratives.Count ? narratives[i].Narrative ?? string.Empty : string.Empty;
            }
        }
        else
        {
            _logger.LogWarning("NarrateReportAsync failed. Falling back to empty narratives for draft. ConnectionId: {ConnectionId}", connectionId);
            foreach (var section in sections)
                section.NarrativeText = string.Empty;
        }

        _logger.LogInformation("Report draft generated. ConnectionId: {ConnectionId}, Sections: {Count}",
            connectionId, sections.Count);

        return new ReportDraftDto
        {
            Title = plan.Title,
            Summary = plan.Summary,
            OriginalPrompt = prompt,
            Sections = sections,
        };
    }

    public async Task<Result<ReportDto>> SaveAsync(
        Guid connectionId,
        string userId,
        CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (access.IsFailure) return Result<ReportDto>.Failure(access.Errors);
        if (!access.Value)
            return Result<ReportDto>.Failure(new Error("reports.forbidden", "You do not have access to this connection."));

        var now = DateTime.UtcNow;
        var report = new Report
        {
            ReportId = Guid.NewGuid(),
            ConnectionId = connectionId,
            UserId = userId,
            Title = request.Title.Trim(),
            OriginalPrompt = request.OriginalPrompt.Trim(),
            Summary = request.Summary?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        var sections = request.Sections.Select((s, idx) =>
        {
            var chartType = ParseChartType(s.ChartType);
            return new ReportSection
            {
                SectionId = Guid.NewGuid(),
                ReportId = report.ReportId,
                SortOrder = s.SortOrder,
                Heading = s.Heading.Trim(),
                NarrativeText = s.NarrativeText,
                ChartType = chartType,
                ChartConfigJson = s.ChartConfigJson,
                SqlQuery = s.SqlQuery?.Trim(),
                CachedResultsJson = s.ResultsJson,
                CachedResultsRowsReturned = s.RowsReturned,
                CachedResultsExecutionTimeMs = s.ExecutionTimeMs,
                CachedResultsSuccess = s.ExecutionSuccess,
                CachedResultsErrorMessage = s.ExecutionErrorMessage,
                CachedResultsExecutedAtUtc = s.ExecutionSuccess.HasValue ? now : null,
            };
        }).ToList();

        _dbContext.Reports.Add(report);
        _dbContext.ReportSections.AddRange(sections);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Report saved. ReportId: {ReportId}, Sections: {Count}", report.ReportId, sections.Count);

        return ToDto(report, sections);
    }

    public async Task<Result<ReportDto>> GetAsync(
        Guid connectionId,
        string userId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var access = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (access.IsFailure) return Result<ReportDto>.Failure(access.Errors);
        if (!access.Value)
            return Result<ReportDto>.Failure(new Error("reports.forbidden", "You do not have access to this connection."));

        var report = await _dbContext.Reports
            .AsNoTracking()
            .Include(r => r.Sections)
            .FirstOrDefaultAsync(r => r.ReportId == reportId && r.ConnectionId == connectionId && r.UserId == userId && !r.IsDeleted, cancellationToken);

        if (report is null)
            return Result<ReportDto>.Failure(new Error("reports.not_found", "Report not found."));

        return ToDto(report, report.Sections.OrderBy(s => s.SortOrder).ToList());
    }

    public async Task<Result<IReadOnlyList<ReportHeaderDto>>> ListAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken)
    {
        var access = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (access.IsFailure) return Result<IReadOnlyList<ReportHeaderDto>>.Failure(access.Errors);
        if (!access.Value)
            return Result<IReadOnlyList<ReportHeaderDto>>.Failure(new Error("reports.forbidden", "You do not have access to this connection."));

        var reports = await _dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ConnectionId == connectionId && r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Select(r => new ReportHeaderDto
            {
                ReportId = r.ReportId,
                ConnectionId = r.ConnectionId,
                Title = r.Title,
                OriginalPrompt = r.OriginalPrompt,
                SectionCount = r.Sections.Count(s => true),
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return reports;
    }

    public async Task<Result<ReportDto>> RefreshAsync(
        Guid connectionId,
        string userId,
        Guid reportId,
        bool regenerateNarrative,
        CancellationToken cancellationToken)
    {
        var access = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (access.IsFailure) return Result<ReportDto>.Failure(access.Errors);
        if (!access.Value)
            return Result<ReportDto>.Failure(new Error("reports.forbidden", "You do not have access to this connection."));

        var report = await _dbContext.Reports
            .Include(r => r.Sections)
            .FirstOrDefaultAsync(r => r.ReportId == reportId && r.ConnectionId == connectionId && r.UserId == userId && !r.IsDeleted, cancellationToken);

        if (report is null)
            return Result<ReportDto>.Failure(new Error("reports.not_found", "Report not found."));

        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
            return Result<ReportDto>.Failure(new Error("reports.connection_not_found", "Connection not found."));

        var executedAt = DateTime.UtcNow;

        var sqlRefreshTasks = report.Sections
            .Where(s => !string.IsNullOrWhiteSpace(s.SqlQuery))
            .Select(async section =>
            {
                var queryResult = await _databaseExecutor.ExecuteQueryAsync(connection, section.SqlQuery!, cancellationToken);
                section.CachedResultsJson = queryResult.ResultsJson;
                section.CachedResultsRowsReturned = queryResult.RowsReturned;
                section.CachedResultsExecutionTimeMs = queryResult.ExecutionTimeMs;
                section.CachedResultsSuccess = queryResult.Success;
                section.CachedResultsErrorMessage = queryResult.ErrorMessage;
                section.CachedResultsExecutedAtUtc = executedAt;
            });

        await Task.WhenAll(sqlRefreshTasks);

        if (regenerateNarrative)
        {
            var orderedSections = report.Sections.OrderBy(s => s.SortOrder).ToList();
            var narrateInputs = orderedSections
                .Select(s => new NarrateReportSectionInputDto
                {
                    Heading = s.Heading,
                    Sql = s.SqlQuery,
                    ChartType = s.ChartType?.ToString()?.ToSnakeCase(),
                    SampleRowsJson = ExtractSampleRows(s.CachedResultsJson, MaxRowsForNarrative),
                })
                .ToList();

            var narrateResult = await _reportAiClient.NarrateReportAsync(
                report.Title,
                report.OriginalPrompt,
                report.Summary,
                narrateInputs,
                cancellationToken);

            if (narrateResult.IsSuccess)
            {
                var narratives = narrateResult.Value ?? [];
                for (var i = 0; i < orderedSections.Count; i++)
                {
                    orderedSections[i].NarrativeText = i < narratives.Count ? narratives[i].Narrative ?? string.Empty : string.Empty;
                }
            }
            else
            {
                foreach (var section in orderedSections)
                    section.NarrativeText = string.Empty;

                _logger.LogWarning(
                    "NarrateReportAsync failed during refresh. Cleared narratives to avoid stale text/data mismatch. ReportId: {ReportId}",
                    reportId);
            }
        }

        report.UpdatedAtUtc = executedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Report refreshed. ReportId: {ReportId}, RegenerateNarrative: {Regen}", reportId, regenerateNarrative);

        return ToDto(report, report.Sections.OrderBy(s => s.SortOrder).ToList());
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid connectionId,
        string userId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var report = await _dbContext.Reports
            .FirstOrDefaultAsync(r => r.ReportId == reportId && r.ConnectionId == connectionId && r.UserId == userId && !r.IsDeleted, cancellationToken);

        if (report is null)
            return Result<bool>.Failure(new Error("reports.not_found", "Report not found."));

        report.IsDeleted = true;
        report.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Report deleted. ReportId: {ReportId}", reportId);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReportDto ToDto(Report report, IEnumerable<ReportSection> sections) => new()
    {
        ReportId = report.ReportId,
        ConnectionId = report.ConnectionId,
        Title = report.Title,
        OriginalPrompt = report.OriginalPrompt,
        Summary = report.Summary,
        CreatedAtUtc = report.CreatedAtUtc,
        UpdatedAtUtc = report.UpdatedAtUtc,
        Sections = sections.Select(s => new ReportSectionDto
        {
            SectionId = s.SectionId,
            SortOrder = s.SortOrder,
            Heading = s.Heading,
            NarrativeText = s.NarrativeText,
            ChartType = s.ChartType?.ToString().ToSnakeCase(),
            ChartConfigJson = s.ChartConfigJson,
            SqlQuery = s.SqlQuery,
            ResultsJson = s.CachedResultsJson,
            RowsReturned = s.CachedResultsRowsReturned,
            ExecutionTimeMs = s.CachedResultsExecutionTimeMs,
            ExecutionSuccess = s.CachedResultsSuccess,
            ExecutionErrorMessage = s.CachedResultsErrorMessage,
            ExecutedAtUtc = s.CachedResultsExecutedAtUtc,
        }).ToList(),
    };

    private static ChartType? ParseChartType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var stripped = raw.Replace("_", "");
        return Enum.TryParse<ChartType>(stripped, ignoreCase: true, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Extracts at most N rows from query results and returns them as a JSON array string.
    /// Supports both raw array payloads and the { columns, rows } envelope format.
    /// </summary>
    private static string ExtractSampleRows(string? resultsJson, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(resultsJson)) return "[]";
        try
        {
            using var doc = JsonDocument.Parse(resultsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var items = doc.RootElement.EnumerateArray().Take(maxRows).Select(x => x.Clone()).ToList();
                return JsonSerializer.Serialize(items);
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("rows", out var rowsEl) &&
                rowsEl.ValueKind == JsonValueKind.Array)
            {
                var rows = rowsEl.EnumerateArray().Take(maxRows).Select(x => x.Clone()).ToList();
                return JsonSerializer.Serialize(rows);
            }

            return "[]";
        }
        catch
        {
            return "[]";
        }
    }
}

// Shared extension — same logic as in ChartService to avoid duplication
file static class ChartTypeSnakeCaseExtension
{
    public static string ToSnakeCase(this string pascalCase)
    {
        var sb = new System.Text.StringBuilder(pascalCase.Length + 4);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var ch = pascalCase[i];
            if (i > 0 && char.IsUpper(ch)) sb.Append('_');
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}
