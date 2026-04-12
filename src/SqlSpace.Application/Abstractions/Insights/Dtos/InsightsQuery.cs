namespace SqlSpace.Application.Abstractions.Insights;

public sealed class InsightsQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public InsightsBucket Bucket { get; set; } = InsightsBucket.Day;
    public int TopN { get; set; } = 5;
}
