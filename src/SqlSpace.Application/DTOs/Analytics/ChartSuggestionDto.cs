namespace SqlSpace.Application.DTOs.Analytics;

public class ChartSuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public string ChartType { get; set; } = "bar";
    public string ChartConfigJson { get; set; } = "{}";
    public string? Insight { get; set; }
}
