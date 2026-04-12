namespace SqlSpace.Application.DTOs.RAG;

/// <summary>
/// Neutral file carrier for the Application layer.
/// The controller maps IFormFile → RagFileUploadDto so the Application layer
/// has no dependency on Microsoft.AspNetCore.Http.
/// </summary>
public class RagFileUploadDto
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Length { get; init; }
}
