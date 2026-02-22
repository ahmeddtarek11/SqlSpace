namespace SqlSpace.Application.Abstractions.Audit;

/// <summary>
/// Paginated audit log result.
/// </summary>
public class PaginatedAuditLogs
{
    public IReadOnlyList<AuditLogDto> Items { get; set; } = new List<AuditLogDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
