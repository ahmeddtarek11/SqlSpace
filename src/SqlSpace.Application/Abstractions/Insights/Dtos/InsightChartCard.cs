namespace SqlSpace.Application.Abstractions.Insights;

public sealed class InsightChartCard
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChartType { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public DateTime? LastUpdatedUtc { get; set; }
    public Guid? QueryHistoryId { get; set; }
    public IReadOnlyList<InsightSeries> Series { get; set; } = new List<InsightSeries>();
}
