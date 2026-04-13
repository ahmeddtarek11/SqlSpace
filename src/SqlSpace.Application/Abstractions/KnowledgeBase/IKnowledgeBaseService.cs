using SqlSpace.Application.DTOs.RAG;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.KnowledgeBase;

public interface IKnowledgeBaseService
{
    /// <summary>
    /// Validates the user is an admin, creates a tracking record, forwards the file
    /// to the Python RAG service, then updates the record with the result.
    /// </summary>
    /// <param name="connectionId">The connection the document belongs to. Becomes the tenant_id sent to Python.</param>
    /// <param name="userId">Authenticated user's ID — used for admin check and as uploaded_by in Python.</param>
    /// <param name="allowedRoles">Roles that can see this document's chunks at query time.</param>
    /// <param name="file">File data mapped from IFormFile in the controller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<RagIngestResultDto>> IngestDocumentAsync(
        Guid connectionId,
        string userId,
        string[] allowedRoles,
        RagFileUploadDto file,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the user has access to the connection, derives their role string,
    /// then forwards the question to the Python RAG service and returns the answer.
    /// </summary>
    /// <param name="connectionId">The connection whose documents are searched.</param>
    /// <param name="userId">Authenticated user's ID — used for access check and role derivation.</param>
    /// <param name="query">Natural-language question from the user.</param>
    /// <param name="topK">Number of similar chunks to retrieve (1–20, default 5).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<RagQueryResultDto>> AskAsync(
        Guid connectionId,
        string userId,
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the user has access to the connection, then returns all non-deleted
    /// documents uploaded for it, ordered newest first.
    /// </summary>
    /// <param name="connectionId">The connection to list documents for.</param>
    /// <param name="userId">Authenticated user's ID — used for access check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<IReadOnlyList<KnowledgeDocument>>> ListDocumentsAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the persisted chat thread for the given (connection, user), ordered
    /// oldest first. Enforces the same access check as <see cref="AskAsync"/>.
    /// </summary>
    /// <param name="connectionId">The connection whose chat thread is fetched.</param>
    /// <param name="userId">Authenticated user's ID — scopes the thread.</param>
    /// <param name="take">Max messages to return. Default 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<IReadOnlyList<KnowledgeChatMessageDto>>> GetChatHistoryAsync(
        Guid connectionId,
        string userId,
        int take = 100,
        CancellationToken cancellationToken = default);
}
