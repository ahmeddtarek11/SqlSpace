using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Query;

/// <summary>
/// Result of executing a text-to-SQL query through the complete pipeline.
/// </summary>
public class QueryExecutionResult
{
    public bool Success { get; set; }
    public Guid QueryHistoryId { get; set; }
    public string GeneratedSql { get; set; } = string.Empty;
    public string? LlmExplanation { get; set; }
    public string? ResultsJson { get; set; }
    public int? RowsReturned { get; set; }
    public long ExecutionTimeMs { get; set; }
    public QueryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
