using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Connection details response (no sensitive data).
/// </summary>
public class ConnectionDto
{
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public DbProviders DatabaseProvider { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? Username { get; set; }
    public bool UseSSL { get; set; }
    public bool UsesRawConnectionString { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime? LastSuccessfulConnection { get; set; }
    public string? LastConnectionError { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
    public string ConnectionSummary { get; set; } = string.Empty;
}
