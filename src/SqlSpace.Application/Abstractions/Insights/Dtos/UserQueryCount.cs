namespace SqlSpace.Application.Abstractions.Insights;

public sealed class UserQueryCount
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int QueryCount { get; set; }
}
