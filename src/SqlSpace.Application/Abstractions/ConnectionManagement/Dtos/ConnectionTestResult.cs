namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Connection test result.
/// </summary>
public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DatabaseName { get; set; }
    public string? ServerVersion { get; set; }
    public int ResponseTimeMs { get; set; }
    public ConnectionComponents? ExtractedComponents { get; set; }
}
