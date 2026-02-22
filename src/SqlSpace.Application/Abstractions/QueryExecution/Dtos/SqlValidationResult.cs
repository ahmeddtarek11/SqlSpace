namespace SqlSpace.Application.DTOs.Query;

/// <summary>
/// Result of SQL validation check.
/// </summary>
public class SqlValidationResult
{
    public bool IsValid { get; set; }
    public bool IsSelectOnly { get; set; }
    public IReadOnlyList<string> TablesReferenced { get; set; } = new List<string>();
    public IReadOnlyList<string> UnauthorizedTables { get; set; } = new List<string>();
    public string? ErrorMessage { get; set; }
}
