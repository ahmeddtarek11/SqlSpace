using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Connection summary for list display.
/// </summary>
public class ConnectionSummaryDto
{
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public DbProviders DatabaseProvider { get; set; }
    public bool IsHealthy { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ConnectionSummary { get; set; } = string.Empty;

    public bool HasFullAccess { get; set; }


}
