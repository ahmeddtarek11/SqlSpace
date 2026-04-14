using SqlSpace.Application.DTOs.RAG;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.SecurityAndLLM;

public interface IRagClient
{
    /// <summary>
    /// Sends a file to the Python RAG service for ingestion.
    /// The Python service extracts text, chunks it, embeds it, and stores vectors in Qdrant.
    /// </summary>
    /// <param name="tenantId">connectionId.ToString() — scopes the document to a database connection.</param>
    /// <param name="uploadedBy">UserId of the uploader, stored as metadata in Python SQLite.</param>
    /// <param name="uploaderRole">Role of the uploader ("admin", "full_access", or "restricted").</param>
    /// <param name="allowedRoles">Roles that can see this document's chunks at query time.</param>
    /// <param name="file">File data mapped from IFormFile in the controller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<RagIngestResultDto>> IngestDocumentAsync(
        string tenantId,
        string uploadedBy,
        string uploaderRole,
        string[] allowedRoles,
        RagFileUploadDto file,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a natural-language question to the Python RAG service.
    /// The service retrieves relevant chunks by similarity, filters by userRole, and generates an answer.
    /// </summary>
    /// <param name="tenantId">connectionId.ToString() — scopes the search to one connection's documents.</param>
    /// <param name="userRole">Derived role of the querying user ("admin", "full_access", or "restricted").</param>
    /// <param name="query">The user's natural-language question.</param>
    /// <param name="topK">Number of similar chunks to retrieve (1–20, default 5).</param>
    /// <param name="fileIds">Optional filter — search only these specific files. Null means all files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<RagQueryResultDto>> QueryAsync(
        string tenantId,
        string userRole,
        string query,
        int topK = 5,
        string[]? fileIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes one ingested file (and all its chunks) from the Python RAG service.
    /// </summary>
    /// <param name="tenantId">connectionId.ToString() — scopes the delete to one tenant.</param>
    /// <param name="fileId">Python RAG file id to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<bool>> DeleteFileAsync(
        string tenantId,
        string fileId,
        CancellationToken cancellationToken = default);
}
