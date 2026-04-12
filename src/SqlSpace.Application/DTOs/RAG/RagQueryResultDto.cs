using System.Text.Json.Serialization;

namespace SqlSpace.Application.DTOs.RAG;

public class RagQueryResultDto
{
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("sources")]
    public IReadOnlyList<RagQuerySourceDto> Sources { get; set; } = new List<RagQuerySourceDto>();

    [JsonPropertyName("tokens_used")]
    public int TokensUsed { get; set; }
}
