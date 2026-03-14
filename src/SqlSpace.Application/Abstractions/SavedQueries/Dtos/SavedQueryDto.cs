namespace SqlSpace.Application.Abstractions.SavedQueries;

public sealed class SavedQueryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public Guid? QueryHistoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
