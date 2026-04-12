namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Parsed connection string components.
/// </summary>
public class ConnectionComponents
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool UseSSL { get; set; }

    public string Password { get; set; } = string.Empty;
    public string? AdditionalParameters { get; set; }
}
