namespace SqlSpace.Application.DTOs.Reports;

public class ReportSectionDto
{
    public Guid SectionId { get; set; }
    public int SortOrder { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string NarrativeText { get; set; } = string.Empty;

    // Nullable — null means text-only section
    public string? ChartType { get; set; }
    public string? ChartConfigJson { get; set; }
    public string? SqlQuery { get; set; }

    // Execution result (null before first run)
    public string? ResultsJson { get; set; }
    public int? RowsReturned { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public bool? ExecutionSuccess { get; set; }
    public string? ExecutionErrorMessage { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
}
