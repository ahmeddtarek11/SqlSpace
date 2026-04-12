namespace SqlSpace.Application.DTOs.AI;

/// <summary>
/// Request to FastAPI for SQL generation.
/// </summary>
public class SqlGenerationRequest
{
    public string UserPrompt { get; set; } = string.Empty;
    public string SchemaContext { get; set; } = string.Empty;
    public IReadOnlyList<string> AccessibleTables { get; set; } = new List<string>();
    public string DatabaseProvider { get; set; } = string.Empty;
    public IReadOnlyList<ConversationMessage>? ConversationHistory { get; set; }
}
