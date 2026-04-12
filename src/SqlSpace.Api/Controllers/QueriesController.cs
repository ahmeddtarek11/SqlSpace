using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Controllers.Query.Dtos;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Query;
using SqlSpace.Application.DTOs.Query;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/queries")]
[Tags("Queries")]
public sealed class QueriesController(
    IQueryExecutionService queryExecutionService,
    IQueryHistoryService queryHistoryService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IQueryExecutionService _queryExecutionService = queryExecutionService;
    private readonly IQueryHistoryService _queryHistoryService = queryHistoryService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("execute")]
    [EndpointSummary("Execute a natural language prompt")]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryExecutionResult>>> ExecutePrompt(
        [FromBody] ExecutePromptRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<QueryExecutionResult>();
        }

        var result = await _queryExecutionService.ExecutePromptAsync(
            request.ConnectionId,
            userId,
            request.UserPrompt,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Query executed.");
    }

    [HttpPost("{queryId:guid}/rerun")]
    [EndpointSummary("Re-run a previously executed query")]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryExecutionResult>>> RerunQuery(
        Guid queryId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<QueryExecutionResult>();
        }

        var result = await _queryExecutionService.RerunQueryAsync(queryId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Query rerun completed.");
    }

    [HttpGet("history")]
    [EndpointSummary("Get query history for the current user")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedQueryHistory>>> GetUserHistory(
        [FromQuery] Guid? connectionId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<PaginatedQueryHistory>();
        }

        var result = await _queryHistoryService.GetUserQueryHistoryAsync(
            userId,
            connectionId,
            pageNumber ?? 1,
            pageSize ?? 20,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Query history loaded.");
    }

    [HttpGet("history/{queryId:guid}")]
    [EndpointSummary("Get a query history record by id")]
    [ProducesResponseType(typeof(ApiResponse<QueryHistoryDetailDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryHistoryDetailDto?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueryHistoryDetailDto?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryHistoryDetailDto?>>> GetQueryById(
        Guid queryId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<QueryHistoryDetailDto?>();
        }

        var result = await _queryHistoryService.GetQueryByIdAsync(queryId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Query history loaded.");
    }

    [HttpGet("history/connection/{connectionId:guid}")]
    [EndpointSummary("Get query history for a connection (admin only)")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedQueryHistory>>> GetConnectionHistory(
        Guid connectionId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<PaginatedQueryHistory>();
        }

        var result = await _queryHistoryService.GetConnectionQueryHistoryAsync(
            connectionId,
            userId,
            pageNumber ?? 1,
            pageSize ?? 20,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Connection query history loaded.");
    }

    [HttpGet("history/search")]
    [EndpointSummary("Search query history by prompt or SQL")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedQueryHistory>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedQueryHistory>>> SearchHistory(
        [FromQuery] string searchTerm,
        [FromQuery] Guid? connectionId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<PaginatedQueryHistory>();
        }

        var result = await _queryHistoryService.SearchQueryHistoryAsync(
            userId,
            searchTerm,
            connectionId,
            pageNumber ?? 1,
            pageSize ?? 20,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Query history search completed.");
    }

    [HttpGet("history/stats")]
    [EndpointSummary("Get query execution statistics for the current user")]
    [ProducesResponseType(typeof(ApiResponse<QueryStatistics>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryStatistics>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueryStatistics>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryStatistics>>> GetUserStatistics(
        [FromQuery] Guid? connectionId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<QueryStatistics>();
        }

        var result = await _queryHistoryService.GetUserQueryStatisticsAsync(
            userId,
            connectionId,
            dateFrom,
            dateTo,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Query statistics loaded.");
    }
}
