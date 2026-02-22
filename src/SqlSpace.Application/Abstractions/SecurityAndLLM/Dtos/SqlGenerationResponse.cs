namespace SqlSpace.Application.DTOs.AI;

/// <summary>
/// Response from FastAPI with generated SQL.
/// </summary>
public class SqlGenerationResponse
{
    public bool Success { get; set; }
    public string GeneratedSql { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool IsValidSql { get; set; }
    public IReadOnlyList<string> TablesReferenced { get; set; } = new List<string>();
    public string? ErrorMessage { get; set; }
}
