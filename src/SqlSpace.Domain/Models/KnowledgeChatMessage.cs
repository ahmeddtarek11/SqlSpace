using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;

public class KnowledgeChatMessage
{
    public Guid MessageId { get; set; }
    public Guid ConnectionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? SourcesJson { get; set; }
    public int? TokensUsed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConnectedDatabase DatabaseConnection { get; set; } = null!;
}
