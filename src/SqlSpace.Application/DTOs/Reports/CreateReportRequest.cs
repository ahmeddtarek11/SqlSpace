namespace SqlSpace.Application.DTOs.Reports;

/// <summary>
/// Sent by the frontend to persist an (optionally edited) draft.
/// </summary>
public class CreateReportRequest
{
    public string Title { get; set; } = string.Empty;
    public string OriginalPrompt { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public IReadOnlyList<CreateReportSectionRequest> Sections { get; set; } = [];
}

public class CreateReportSectionRequest
{
    public int SortOrder { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string NarrativeText { get; set; } = string.Empty;
    public string? ChartType { get; set; }
    public string? ChartConfigJson { get; set; }
    public string? SqlQuery { get; set; }
    // Results from the draft run are also passed through so they show immediately after save
    public string? ResultsJson { get; set; }
    public int? RowsReturned { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public bool? ExecutionSuccess { get; set; }
    public string? ExecutionErrorMessage { get; set; }
}
