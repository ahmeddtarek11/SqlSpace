using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.KnowledgeBase;
using SqlSpace.Application.Abstractions.SecurityAndLLM;
using SqlSpace.Application.DTOs.RAG;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.KnowledgeBase;

public class KnowledgeBaseService(ILogger<KnowledgeBaseService> logger , IRagClient ragClient , IApplicationDbContext dbContext , IAccessControlService accessControlService) : IKnowledgeBaseService
{
    private readonly ILogger<KnowledgeBaseService> _logger = logger;

    private readonly IRagClient _ragClient = ragClient;

    private readonly IApplicationDbContext _dbContext = dbContext;

    private readonly IAccessControlService _accessControlService = accessControlService;


    private const int ChatContextMessageCount = 10;

    public async Task<Result<RagQueryResultDto>> AskAsync(Guid connectionId, string userId, string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        // 1. verify the user has access to this connection
        var hasAccess = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (hasAccess.IsFailure)
            return Result<RagQueryResultDto>.Failure(hasAccess.Errors);

        if (!hasAccess.Value)
            return Result<RagQueryResultDto>.Failure(
                new Error("rag.forbidden", "You do not have access to this connection."));

        // 2. derive the user's role for the Python RAG service
        var isAdmin = await _accessControlService.IsAdmin(connectionId, userId);
        string userRole;

        if (isAdmin.IsSuccess && isAdmin.Value)
        {
            userRole = "admin";
        }
        else
        {
            var access = await _dbContext.UserDatabaseAccesses
                .FirstOrDefaultAsync(
                    a => a.UserId == userId &&
                         a.DatabaseConnectionId == connectionId &&
                         !a.IsDeleted,
                    cancellationToken);

            userRole = access?.HasFullAccess is true ? "full_access" : "restricted";
        }

        // 3. load recent chat history (for multi-turn context)
        var recentMessages = await _dbContext.KnowledgeChatMessages
            .Where(m => m.ConnectionId == connectionId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.MessageId)
            .Take(ChatContextMessageCount)
            .ToListAsync(cancellationToken);

        recentMessages = OrderMessagesChronologically(recentMessages);

        var composedQuery = BuildComposedQuery(recentMessages, query);

        // 4. forward to the Python RAG service
        _logger.LogInformation(
            "RAG query. ConnectionId: {ConnectionId}, Role: {Role}, TopK: {TopK}, PriorMessages: {Prior}",
            connectionId, userRole, topK, recentMessages.Count);

        var ragResult = await _ragClient.QueryAsync(
            tenantId: connectionId.ToString(),
            userRole: userRole,
            query: composedQuery,
            topK: topK,
            cancellationToken: cancellationToken);

        // 5. persist the user question + assistant reply as a pair
        var now = DateTime.UtcNow;

        var userMessage = new KnowledgeChatMessage
        {
            MessageId    = Guid.NewGuid(),
            ConnectionId = connectionId,
            UserId       = userId,
            Role         = ChatMessageRole.User,
            Content      = query,
            CreatedAt    = now,
        };

        var assistantMessage = new KnowledgeChatMessage
        {
            MessageId    = Guid.NewGuid(),
            ConnectionId = connectionId,
            UserId       = userId,
            Role         = ChatMessageRole.Assistant,
            CreatedAt    = now.AddTicks(1),
        };

        if (ragResult.IsSuccess && ragResult.Value is not null)
        {
            assistantMessage.Content     = ragResult.Value.Answer ?? string.Empty;
            assistantMessage.TokensUsed  = ragResult.Value.TokensUsed;
            assistantMessage.SourcesJson = ragResult.Value.Sources is { Count: > 0 }
                ? JsonSerializer.Serialize(ragResult.Value.Sources)
                : null;
        }
        else
        {
            assistantMessage.Content      = string.Empty;
            assistantMessage.ErrorMessage = ragResult.Errors.Count > 0
                ? ragResult.Errors[0].Message
                : "Unknown RAG error.";
        }

        _dbContext.KnowledgeChatMessages.Add(userMessage);
        _dbContext.KnowledgeChatMessages.Add(assistantMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ragResult;
    }

    private static IReadOnlyList<ChatMessageSourceDto>? DeserializeSources(string? sourcesJson)
    {
        if (string.IsNullOrWhiteSpace(sourcesJson))
            return null;

        try
        {
            var raw = JsonSerializer.Deserialize<List<RagQuerySourceDto>>(sourcesJson);
            if (raw is null || raw.Count == 0)
                return null;

            return raw.Select(s => new ChatMessageSourceDto
            {
                FileId         = s.FileId,
                FileName       = s.FileName,
                ChunkId        = s.ChunkId,
                RelevanceScore = s.RelevanceScore,
                Excerpt        = s.Excerpt,
            }).ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildComposedQuery(IReadOnlyList<KnowledgeChatMessage> history, string currentQuery)
    {
        if (history.Count == 0)
            return currentQuery;

        var sb = new StringBuilder();
        sb.AppendLine("Prior conversation (for context, do not repeat):");
        foreach (var msg in history)
        {
            if (string.IsNullOrWhiteSpace(msg.Content))
                continue;

            var label = msg.Role == ChatMessageRole.User ? "User" : "Assistant";
            sb.Append(label).Append(": ").AppendLine(msg.Content);
        }
        sb.AppendLine();
        sb.Append("Current question: ").Append(currentQuery);
        return sb.ToString();
    }

    private static List<KnowledgeChatMessage> OrderMessagesChronologically(IEnumerable<KnowledgeChatMessage> messages)
    {
        return messages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Role == ChatMessageRole.User ? 0 : 1)
            .ThenBy(m => m.MessageId)
            .ToList();
    }

    public async Task<Result<IReadOnlyList<KnowledgeChatMessageDto>>> GetChatHistoryAsync(
        Guid connectionId,
        string userId,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (hasAccess.IsFailure)
            return Result<IReadOnlyList<KnowledgeChatMessageDto>>.Failure(hasAccess.Errors);

        if (!hasAccess.Value)
            return Result<IReadOnlyList<KnowledgeChatMessageDto>>.Failure(
                new Error("rag.forbidden", "You do not have access to this connection."));

        var safeTake = Math.Clamp(take, 1, 500);

        var messages = await _dbContext.KnowledgeChatMessages
            .Where(m => m.ConnectionId == connectionId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.MessageId)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        messages = OrderMessagesChronologically(messages);

        var dtos = messages.Select(m => new KnowledgeChatMessageDto
        {
            MessageId    = m.MessageId,
            Role         = m.Role == ChatMessageRole.User ? "user" : "assistant",
            Content      = m.Content,
            TokensUsed   = m.TokensUsed,
            ErrorMessage = m.ErrorMessage,
            CreatedAt    = m.CreatedAt,
            Sources      = DeserializeSources(m.SourcesJson),
        }).ToList();

        return Result<IReadOnlyList<KnowledgeChatMessageDto>>.Success(dtos);
    }

    public async Task<Result<RagIngestResultDto>> IngestDocumentAsync(Guid connectionId, string userId, string[] allowedRoles, RagFileUploadDto file, CancellationToken cancellationToken)
    {
        // 1. only the connection admin can upload documents
        var isAdmin = await _accessControlService.IsAdmin(connectionId, userId);
        if (isAdmin.IsFailure)
            return Result<RagIngestResultDto>.Failure(isAdmin.Errors);

        if (!isAdmin.Value)
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.forbidden", "Only connection admins can upload documents."));

        // 2. call the Python RAG service
        _logger.LogInformation(
            "Ingesting document. File: {FileName}, ConnectionId: {ConnectionId}",
            file.FileName, connectionId);

        var ragResult = await _ragClient.IngestDocumentAsync(
            tenantId:     connectionId.ToString(),
            uploadedBy:   userId,
            uploaderRole: "admin",
            allowedRoles: allowedRoles,
            file:         file,
            cancellationToken: cancellationToken);

        // 3. upsert the tracking record with the outcome
        if (ragResult.IsSuccess && ragResult.Value is not null)
        {
            var now = DateTime.UtcNow;
            var existingDocument = await _dbContext.KnowledgeDocuments
                .Where(d => d.ConnectionId == connectionId &&
                            !d.IsDeleted &&
                            d.PythonFileId == ragResult.Value.FileId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingDocument is null)
            {
                var indexedDocument = new KnowledgeDocument
                {
                    DocumentId       = Guid.NewGuid(),
                    ConnectionId     = connectionId,
                    UploadedByUserId = userId,
                    FileName         = ragResult.Value.FileName,
                    Status           = KnowledgeDocumentStatus.Indexed,
                    PythonFileId     = ragResult.Value.FileId,
                    ChunksCreated    = ragResult.Value.ChunksCreated,
                    ProcessedAt      = now,
                };

                _dbContext.KnowledgeDocuments.Add(indexedDocument);
            }
            else
            {
                existingDocument.FileName = ragResult.Value.FileName;
                existingDocument.Status = KnowledgeDocumentStatus.Indexed;
                existingDocument.ErrorMessage = null;
                existingDocument.ChunksCreated = ragResult.Value.ChunksCreated;
                existingDocument.ProcessedAt = now;
            }
        }
        else
        {
            var failedDocument = new KnowledgeDocument
            {
                DocumentId       = Guid.NewGuid(),
                ConnectionId     = connectionId,
                UploadedByUserId = userId,
                FileName         = file.FileName,
                Status           = KnowledgeDocumentStatus.Failed,
                ErrorMessage     = ragResult.Errors[0].Message,
                ProcessedAt      = DateTime.UtcNow,
            };

            _dbContext.KnowledgeDocuments.Add(failedDocument);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ragResult;
    }

    public async Task<Result<IReadOnlyList<KnowledgeDocument>>> ListDocumentsAsync(Guid connectionId, string userId, CancellationToken cancellationToken)
    {
        // 1. verify the user has access to this connection
        var hasAccess = await _accessControlService.HasAccessToConnectionAsync(connectionId, userId, cancellationToken);
        if (hasAccess.IsFailure)
            return Result<IReadOnlyList<KnowledgeDocument>>.Failure(hasAccess.Errors);

        if (!hasAccess.Value)
            return Result<IReadOnlyList<KnowledgeDocument>>.Failure(
                new Error("rag.forbidden", "You do not have access to this connection."));

        // 2. query active documents only
        var documents = await _dbContext.KnowledgeDocuments
            .Where(d => d.ConnectionId == connectionId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<KnowledgeDocument>>.Success(documents);
    }

    public async Task<Result<bool>> DeleteDocumentAsync(
        Guid connectionId,
        string userId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var isAdmin = await _accessControlService.IsAdmin(connectionId, userId);
        if (isAdmin.IsFailure)
            return Result<bool>.Failure(isAdmin.Errors);

        if (!isAdmin.Value)
            return Result<bool>.Failure(
                new Error("rag.forbidden", "Only connection admins can delete documents."));

        var document = await _dbContext.KnowledgeDocuments
            .FirstOrDefaultAsync(
                d => d.DocumentId == documentId &&
                     d.ConnectionId == connectionId &&
                     !d.IsDeleted,
                cancellationToken);

        if (document is null)
            return Result<bool>.Failure(
                new Error("rag.not_found", "Document not found."));

        if (!string.IsNullOrWhiteSpace(document.PythonFileId))
        {
            var ragDelete = await _ragClient.DeleteFileAsync(
                tenantId: connectionId.ToString(),
                fileId: document.PythonFileId,
                cancellationToken: cancellationToken);

            var remoteFileMissing = ragDelete.IsFailure && ragDelete.Errors.Any(
                e => string.Equals(e.Code, "rag.not_found", StringComparison.OrdinalIgnoreCase));

            if (ragDelete.IsFailure && !remoteFileMissing)
                return Result<bool>.Failure(ragDelete.Errors);
        }

        document.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Knowledge document deleted. DocumentId: {DocumentId}, ConnectionId: {ConnectionId}",
            documentId,
            connectionId);

        return Result<bool>.Success(true);
    }

}
