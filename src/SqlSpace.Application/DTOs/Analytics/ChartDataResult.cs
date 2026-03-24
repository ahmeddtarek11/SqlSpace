namespace SqlSpace.Application.DTOs.Analytics;

public class ChartDataResult
{
    public Guid ChartId { get; set; }
    public bool Success { get; set; }
    public string? ResultsJson { get; set; }
    public int RowsReturned { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}
