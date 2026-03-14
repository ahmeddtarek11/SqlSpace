namespace SqlSpace.Application.Abstractions.Insights;

public sealed class InsightsSummary
{
    public int TotalQueries { get; set; }
    public int SuccessfulQueries { get; set; }
    public int FailedQueries { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public long TotalRowsReturned { get; set; }
    public DateTime? FirstQueryDate { get; set; }
    public DateTime? LastQueryDate { get; set; }
}
