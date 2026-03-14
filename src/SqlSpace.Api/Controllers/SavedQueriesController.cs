using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Controllers.SavedQueries.Dtos;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.SavedQueries;
using SqlSpace.Application.DTOs.Query;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/saved-queries")]
[Tags("Saved Queries")]
public sealed class SavedQueriesController(
    ISavedQueryService savedQueryService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly ISavedQueryService _savedQueryService = savedQueryService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpGet]
    [EndpointSummary("List saved queries for the current user")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SavedQueryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SavedQueryDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedQueryDto>>>> GetSavedQueries(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<IReadOnlyList<SavedQueryDto>>();
        }

        var result = await _savedQueryService.GetSavedQueriesAsync(userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Saved queries loaded.");
    }

    [HttpPost]
    [EndpointSummary("Create a saved query")]
    [ProducesResponseType(typeof(ApiResponse<SavedQueryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SavedQueryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SavedQueryDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> CreateSavedQuery(
        [FromBody] CreateSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<SavedQueryDto>();
        }

        var result = await _savedQueryService.CreateSavedQueryAsync(request, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status201Created, "Saved query created.");
    }

    [HttpPatch("{id:guid}")]
    [EndpointSummary("Rename a saved query")]
    [ProducesResponseType(typeof(ApiResponse<SavedQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SavedQueryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SavedQueryDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> RenameSavedQuery(
        Guid id,
        [FromBody] RenameSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<SavedQueryDto>();
        }

        var result = await _savedQueryService.RenameSavedQueryAsync(id, request.Name, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Saved query updated.");
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a saved query")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSavedQuery(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<bool>();
        }

        var result = await _savedQueryService.DeleteSavedQueryAsync(id, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Saved query deleted.");
    }

    [HttpPost("{id:guid}/execute")]
    [EndpointSummary("Execute a saved query")]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueryExecutionResult>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryExecutionResult>>> ExecuteSavedQuery(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<QueryExecutionResult>();
        }

        var result = await _savedQueryService.ExecuteSavedQueryAsync(id, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Saved query executed.");
    }
}
