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
        //    admin  → "admin"
        //    full access granted → "full_access"
        //    restricted access → "restricted"
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

        // 3. forward the question to the Python RAG service
        _logger.LogInformation(
            "RAG query. ConnectionId: {ConnectionId}, Role: {Role}, TopK: {TopK}",
            connectionId, userRole, topK);

        return await _ragClient.QueryAsync(
            tenantId: connectionId.ToString(),
            userRole: userRole,
            query: query,
            topK: topK,
            cancellationToken: cancellationToken);
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

        // 2. create a tracking record before calling Python
        //    so we always have a row even if Python fails mid-flight
        var document = new KnowledgeDocument
        {
            DocumentId        = Guid.NewGuid(),
            ConnectionId      = connectionId,
            UploadedByUserId  = userId,
            FileName          = file.FileName,
            Status            = KnowledgeDocumentStatus.Processing,
        };

        _dbContext.KnowledgeDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Ingesting document. DocumentId: {DocumentId}, File: {FileName}, ConnectionId: {ConnectionId}",
            document.DocumentId, file.FileName, connectionId);

        // 3. call the Python RAG service
        var ragResult = await _ragClient.IngestDocumentAsync(
            tenantId:     connectionId.ToString(),
            uploadedBy:   userId,
            uploaderRole: "admin",
            allowedRoles: allowedRoles,
            file:         file,
            cancellationToken: cancellationToken);

        // 4. update the tracking record with the outcome
        if (ragResult.IsSuccess)
        {
            document.Status        = KnowledgeDocumentStatus.Indexed;
            document.PythonFileId  = ragResult.Value!.FileId;
            document.ChunksCreated = ragResult.Value!.ChunksCreated;
            document.ProcessedAt   = DateTime.UtcNow;
        }
        else
        {
            document.Status       = KnowledgeDocumentStatus.Failed;
            document.ErrorMessage = ragResult.Errors[0].Message;
            document.ProcessedAt  = DateTime.UtcNow;
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

        // 2. query documents — EF global query filter already excludes IsDeleted = true
        var documents = await _dbContext.KnowledgeDocuments
            .Where(d => d.ConnectionId == connectionId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<KnowledgeDocument>>.Success(documents);
    }

}
