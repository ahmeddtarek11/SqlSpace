namespace SqlSpace.Application.DTOs.Query;

/// <summary>
/// Result of executing SQL on external database.
/// </summary>
public class DatabaseQueryResult
{
    public bool Success { get; set; }
    public string? ResultsJson { get; set; }
    public int RowsReturned { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}
