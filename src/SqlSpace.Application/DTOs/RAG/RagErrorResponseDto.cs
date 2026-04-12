using System.Text.Json.Serialization;

namespace SqlSpace.Application.DTOs.RAG;

public class RagErrorResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("error_subcode")]
    public string? ErrorSubcode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
