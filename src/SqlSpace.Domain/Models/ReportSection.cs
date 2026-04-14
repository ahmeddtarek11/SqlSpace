using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;

public class ReportSection
{
    public Guid SectionId { get; set; }
    public Guid ReportId { get; set; }

    public int SortOrder { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string NarrativeText { get; set; } = string.Empty;

    // Chart config — null means text-only section
    public ChartType? ChartType { get; set; }
    public string? ChartConfigJson { get; set; }
    public string? SqlQuery { get; set; }

    // Cached last execution result
    public string? CachedResultsJson { get; set; }
    public int? CachedResultsRowsReturned { get; set; }
    public long? CachedResultsExecutionTimeMs { get; set; }
    public bool? CachedResultsSuccess { get; set; }
    public string? CachedResultsErrorMessage { get; set; }
    public DateTime? CachedResultsExecutedAtUtc { get; set; }

    // Navigation
    public Report Report { get; set; } = null!;
}
