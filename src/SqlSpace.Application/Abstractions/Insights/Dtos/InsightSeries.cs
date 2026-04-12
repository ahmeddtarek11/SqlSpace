namespace SqlSpace.Application.Abstractions.Insights;

public sealed class InsightSeries
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<InsightPoint> Points { get; set; } = new List<InsightPoint>();
}
