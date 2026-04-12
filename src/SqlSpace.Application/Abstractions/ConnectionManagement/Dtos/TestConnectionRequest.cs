using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Request to test database connection.
/// </summary>
public class TestConnectionRequest
{
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
