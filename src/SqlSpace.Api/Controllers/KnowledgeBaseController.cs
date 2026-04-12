using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.KnowledgeBase;
using SqlSpace.Application.DTOs.RAG;
using SqlSpace.Domain.Models;

namespace SqlSpace.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/connections/{connectionId:guid}/knowledge")]
[Tags("Knowledge Base")]
public class KnowledgeBaseController(
    IKnowledgeBaseService knowledgeBaseService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IKnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Upload a document to the knowledge base")]
    [EndpointDescription("Admin only. Uploads a PDF, DOCX, or TXT file. The file is chunked and embedded by the Python RAG service. allowedRoles controls which access levels can see chunks from this document at query time.")]
    [ProducesResponseType(typeof(ApiResponse<RagIngestResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RagIngestResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RagIngestResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RagIngestResultDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<RagIngestResultDto>>> UploadDocument(
        Guid connectionId,
        IFormFile file,
        [FromForm] string[] allowedRoles,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<RagIngestResultDto>();

        var fileDto = new RagFileUploadDto
        {
            Content     = file.OpenReadStream(),
            FileName    = file.FileName,
            ContentType = file.ContentType,
            Length      = file.Length
        };

        var result = await _knowledgeBaseService.IngestDocumentAsync(
            connectionId, userId, allowedRoles, fileDto, cancellationToken);

        return ToApiResponse(result, StatusCodes.Status201Created, "Document uploaded and indexed.");
    }

    [HttpGet("documents")]
    [EndpointSummary("List documents uploaded to the knowledge base")]
    [EndpointDescription("Returns all non-deleted documents for this connection, ordered newest first. Accessible by any user with connection access.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KnowledgeDocument>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KnowledgeDocument>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<KnowledgeDocument>>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeDocument>>>> ListDocuments(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<IReadOnlyList<KnowledgeDocument>>();

        var result = await _knowledgeBaseService.ListDocumentsAsync(
            connectionId, userId, cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Documents retrieved.");
    }

    [HttpPost("ask")]
    [EndpointSummary("Ask a question against the knowledge base")]
    [EndpointDescription("The answer is generated from document chunks the user's role is allowed to see. Role is derived automatically from the user's access level on this connection.")]
    [ProducesResponseType(typeof(ApiResponse<RagQueryResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RagQueryResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RagQueryResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RagQueryResultDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<RagQueryResultDto>>> Ask(
        Guid connectionId,
        [FromBody] KnowledgeAskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<RagQueryResultDto>();

        var result = await _knowledgeBaseService.AskAsync(
            connectionId, userId, request.Query, request.TopK, cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Query answered.");
    }
}

public class KnowledgeAskRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}
