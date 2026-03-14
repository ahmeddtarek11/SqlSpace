using System;

namespace SqlSpace.Domain.Models;

public class SavedQuery
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid DatabaseConnectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public Guid? QueryHistoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ConnectedDatabase DatabaseConnection { get; set; } = null!;
}
