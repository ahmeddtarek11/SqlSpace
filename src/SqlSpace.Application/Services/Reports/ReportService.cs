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

    private const int MaxSections = 4;
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

        // 2. Execute SQL for each section that has it
        var sections = new List<ReportSectionDto>();
        for (var i = 0; i < plan.Sections.Count; i++)
        {
            var planned = plan.Sections[i];
            var section = new ReportSectionDto
            {
                SectionId = Guid.NewGuid(),
                SortOrder = i,
                Heading = planned.Heading,
                ChartType = planned.ChartType,
                ChartConfigJson = planned.ChartConfig,
                SqlQuery = planned.Sql,
            };

            if (!string.IsNullOrWhiteSpace(planned.Sql))
            {
                var queryResult = await _databaseExecutor.ExecuteQueryAsync(connection, planned.Sql, cancellationToken);
                section.ExecutionSuccess = queryResult.Success;
                section.ResultsJson = queryResult.ResultsJson;
                section.RowsReturned = queryResult.RowsReturned;
                section.ExecutionTimeMs = queryResult.ExecutionTimeMs;
                section.ExecutionErrorMessage = queryResult.ErrorMessage;
                section.ExecutedAtUtc = DateTime.UtcNow;
            }

            sections.Add(section);
        }

        // 3. Narrate each section using real data
        foreach (var section in sections)
        {
            var isSqlBackedSection = !string.IsNullOrWhiteSpace(section.SqlQuery);
            var hasNarratableSqlResult = section.ExecutionSuccess == true && section.RowsReturned.GetValueOrDefault() > 0;
            if (isSqlBackedSection && !hasNarratableSqlResult)
            {
                section.NarrativeText = string.Empty;
                continue;
            }

            var sampleJson = ExtractSampleRows(section.ResultsJson, MaxRowsForNarrative);
            var narrateResult = await _reportAiClient.NarrateSectionAsync(
                section.Heading, prompt, section.SqlQuery, sampleJson, cancellationToken);

            section.NarrativeText = narrateResult.IsSuccess ? narrateResult.Value ?? string.Empty : string.Empty;
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

        foreach (var section in report.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.SqlQuery)) continue;

            var queryResult = await _databaseExecutor.ExecuteQueryAsync(connection, section.SqlQuery, cancellationToken);
            section.CachedResultsJson = queryResult.ResultsJson;
            section.CachedResultsRowsReturned = queryResult.RowsReturned;
            section.CachedResultsExecutionTimeMs = queryResult.ExecutionTimeMs;
            section.CachedResultsSuccess = queryResult.Success;
            section.CachedResultsErrorMessage = queryResult.ErrorMessage;
            section.CachedResultsExecutedAtUtc = executedAt;

            if (regenerateNarrative)
            {
                if (!queryResult.Success || queryResult.RowsReturned == 0)
                {
                    section.NarrativeText = string.Empty;
                    continue;
                }

                var sampleJson = ExtractSampleRows(queryResult.ResultsJson, MaxRowsForNarrative);
                var narrateResult = await _reportAiClient.NarrateSectionAsync(
                    section.Heading, report.OriginalPrompt, section.SqlQuery, sampleJson, cancellationToken);
                if (narrateResult.IsSuccess && !string.IsNullOrEmpty(narrateResult.Value))
                    section.NarrativeText = narrateResult.Value;
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
    /// Extracts the first N rows from a ResultsJson array string, for use as LLM narrative context.
    /// </summary>
    private static string? ExtractSampleRows(string? resultsJson, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(resultsJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(resultsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return resultsJson;
            var items = doc.RootElement.EnumerateArray().Take(maxRows).ToList();
            return JsonSerializer.Serialize(items);
        }
        catch
        {
            return null;
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
