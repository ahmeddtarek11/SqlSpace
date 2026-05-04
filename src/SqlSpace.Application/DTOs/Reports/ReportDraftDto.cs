namespace SqlSpace.Application.DTOs.Reports;

/// <summary>
/// In-memory only. Returned from POST /reports/draft. No DB row exists yet.
/// </summary>
public class ReportDraftDto
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string OriginalPrompt { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<ReportSectionDto> Sections { get; set; } = [];
}
