using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Abstractions.Audit;

/// <summary>
/// Audit log entry for display.
/// </summary>
public class AuditLogDto
{
    public Guid AuditLogId { get; set; }
    public string ActorUserEmail { get; set; } = string.Empty;
    public string  ActorUserName { get; set; } = string.Empty;
    public string TargetUserEmail { get; set; } = string.Empty;
    public string  TargetUserName { get; set; } = string.Empty;
    public AccessAuditLogActions Action { get; set; }
    public string? Details { get; set; }
    public DateTime PerformedAt { get; set; }
}
