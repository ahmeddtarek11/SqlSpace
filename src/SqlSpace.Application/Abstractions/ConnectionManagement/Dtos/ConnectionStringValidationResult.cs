namespace SqlSpace.Application.DTOs.Connection;

/// <summary>
/// Connection string validation result.
/// </summary>
public class ConnectionStringValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ConnectionComponents? ParsedComponents { get; set; }
}
