namespace SqlSpace.Application.DTOs.Analytics;

public class SavedChartDto
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SqlQuery { get; set; } = string.Empty;
    public string? OriginalPrompt { get; set; }
    public string ChartType { get; set; } = "bar";
    public string ChartConfigJson { get; set; } = "{}";
    public string? Insight { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridW { get; set; }
    public int GridH { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
