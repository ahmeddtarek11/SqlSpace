namespace SqlSpace.Application.DTOs.Query;

/// <summary>
/// Query execution statistics.
/// </summary>
public class QueryStatistics
{
    public int TotalQueries { get; set; }
    public int SuccessfulQueries { get; set; }
    public int FailedQueries { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public long TotalRowsReturned { get; set; }
    public IReadOnlyList<TableQueryCount> MostQueriedTables { get; set; } = new List<TableQueryCount>();
    public DateTime? FirstQueryDate { get; set; }
    public DateTime? LastQueryDate { get; set; }
}
