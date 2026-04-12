using System.Text.Json.Serialization;

namespace SqlSpace.Application.DTOs.RAG;

public class RagQuerySourceDto
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("chunk_id")]
    public string ChunkId { get; set; } = string.Empty;

    [JsonPropertyName("relevance_score")]
    public float RelevanceScore { get; set; }

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;
}
