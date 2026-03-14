namespace SqlSpace.Application.Abstractions.Insights;

public sealed class InsightPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}
