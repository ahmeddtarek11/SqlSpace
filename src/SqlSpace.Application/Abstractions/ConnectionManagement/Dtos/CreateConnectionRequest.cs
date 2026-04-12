using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Request to create a new database connection.
/// </summary>
public class CreateConnectionRequest
{
    public string ConnectionName { get; set; } = string.Empty;
    public DbProviders DatabaseProvider { get; set; }
    public ConnectionInputMode InputMode { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSSL { get; set; } = true;
    public string? AdditionalParameters { get; set; }
    public string? RawConnectionString { get; set; }
}
