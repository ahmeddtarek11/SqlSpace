using System.Text.Json.Serialization;

namespace SqlSpace.Application.DTOs.RAG;

public class RagIngestResultDto
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("chunks_created")]
    public int ChunksCreated { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
