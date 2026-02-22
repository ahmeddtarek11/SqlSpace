using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Query;

/// <summary>
/// Query history summary for list display.
/// </summary>
public class QueryHistoryDto
{
    public Guid QueryId { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public QueryStatus Status { get; set; }
    public int? RowsReturned { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
}
