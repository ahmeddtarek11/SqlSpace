namespace SqlSpace.Application.DTOs.AI;

/// <summary>
/// Conversation message for LLM context.
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
