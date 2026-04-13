namespace SqlSpace.Application.DTOs.RAG;

public class KnowledgeChatMessageDto
{
    public Guid MessageId { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public IReadOnlyList<ChatMessageSourceDto>? Sources { get; set; }
    public int? TokensUsed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChatMessageSourceDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public string Excerpt { get; set; } = string.Empty;
}
