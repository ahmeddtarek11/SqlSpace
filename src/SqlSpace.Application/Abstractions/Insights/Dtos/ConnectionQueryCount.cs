namespace SqlSpace.Application.Abstractions.Insights;

public sealed class ConnectionQueryCount
{
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public int QueryCount { get; set; }
}
