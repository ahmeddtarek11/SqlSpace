using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Query;

/// <summary>
/// Detailed query history record.
/// </summary>
public class QueryHistoryDetailDto
{
    public Guid QueryId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public string? LlmResponse { get; set; }
    public QueryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultsJson { get; set; }
    public int? RowsReturned { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; }
    public bool WasAdminAtExecution { get; set; }
}
