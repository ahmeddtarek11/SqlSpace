namespace SqlSpace.Application.DTOs.Analytics;

public class SaveChartRequest
{
    public Guid ConnectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SqlQuery { get; set; } = string.Empty;
    public string? OriginalPrompt { get; set; }
    public string ChartType { get; set; } = "bar";
    public string ChartConfigJson { get; set; } = "{}";
    public string? Insight { get; set; }
    public int GridX { get; set; } = 0;
    public int GridY { get; set; } = 0;
    public int GridW { get; set; } = 6;
    public int GridH { get; set; } = 4;
}
