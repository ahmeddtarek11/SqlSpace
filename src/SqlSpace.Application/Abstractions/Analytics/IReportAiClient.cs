using SqlSpace.Application.DTOs.Reports;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Analytics;

public interface IReportAiClient
{
    /// <summary>
    /// Calls Python /plan-report. Returns a structured list of sections with headings,
    /// SQL, chart types — no narrative text yet.
    /// </summary>
    Task<Result<PlanReportResponseDto>> PlanReportAsync(
        string schemaContext,
        string databaseProvider,
        string userPrompt,
        int maxSections,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calls Python /narrate-section. Given a section heading, the SQL, and sample rows
    /// (JSON string), returns a 2-4 sentence insight paragraph grounded in the real data.
    /// </summary>
    Task<Result<string>> NarrateSectionAsync(
        string heading,
        string userPrompt,
        string? sql,
        string? sampleRowsJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calls Python /narrate-report once for all sections in a report draft.
    /// Returns one narrative entry per requested section.
    /// </summary>
    Task<Result<IReadOnlyList<NarratedSectionDto>>> NarrateReportAsync(
        string title,
        string userPrompt,
        string? summary,
        IReadOnlyList<NarrateReportSectionInputDto> sections,
        CancellationToken cancellationToken);
}
