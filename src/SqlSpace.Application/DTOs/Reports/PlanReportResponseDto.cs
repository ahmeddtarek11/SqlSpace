namespace SqlSpace.Application.DTOs.Reports;

/// <summary>
/// Wire shapes for the Python AI client calls.
/// </summary>

public class PlanReportResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public IReadOnlyList<PlannedSectionDto> Sections { get; set; } = [];
}

public class PlannedSectionDto
{
    public string Heading { get; set; } = string.Empty;
    public string? Sql { get; set; }
    public string? ChartType { get; set; }
    public string? ChartConfig { get; set; }
}
