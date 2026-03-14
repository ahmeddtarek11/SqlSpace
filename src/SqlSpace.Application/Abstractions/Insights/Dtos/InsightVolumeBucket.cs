namespace SqlSpace.Application.Abstractions.Insights;

public sealed class InsightVolumeBucket
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
}
