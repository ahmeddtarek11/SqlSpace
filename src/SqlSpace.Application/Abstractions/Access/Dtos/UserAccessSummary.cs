namespace SqlSpace.Application.Abstractions.Access;

/// <summary>
/// User access summary for display.
/// </summary>
public class UserAccessSummary
{
    public Guid AccessId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool HasFullAccess { get; set; }
    public IReadOnlyList<string> RestrictedTables { get; set; } = new List<string>();
    public DateTime GrantedAt { get; set; }
    public string GrantedByUserEmail { get; set; } = string.Empty;
}
