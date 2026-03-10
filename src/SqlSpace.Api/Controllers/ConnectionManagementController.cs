using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Controllers.ConnectionManagement.Dtos;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.ConnectionManagement.Dtos;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/connections")]
[Tags("Connections")]
public sealed class ConnectionManagementController(
    IConnectionManagementService connectionManagementService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IConnectionManagementService _connectionManagementService = connectionManagementService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpGet]
    [EndpointSummary("List current user's accessible connections")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConnectionSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConnectionSummaryDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConnectionSummaryDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConnectionSummaryDto>>>> GetUserConnections(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<IReadOnlyList<ConnectionSummaryDto>>();
        }

        var result = await _connectionManagementService.GetUserConnectionsAsync(userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Connections loaded.");
    }

    [HttpGet("{connectionId:guid}")]
    [EndpointSummary("Get connection details")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ConnectionDto?>>> GetConnectionById(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<ConnectionDto?>();
        }

        var result = await _connectionManagementService.GetConnectionByIdAsync(connectionId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Connection loaded.");
    }

    [HttpPost]
    [EndpointSummary("Create a new connection")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionCreationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionCreationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionCreationResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ConnectionCreationResponse>>> CreateConnection(
        [FromBody] CreateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<ConnectionCreationResponse>();
        }

        var result = await _connectionManagementService.CreateConnectionAsync(userId, request, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status201Created, "Connection created.");
    }

    [HttpPost("test")]
    [EndpointSummary("Test a new connection without persisting it")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResult>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ConnectionTestResult>>> TestNewConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _connectionManagementService.TestNewConnectionAsync(request, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Connection test completed.");
    }

    [HttpPatch("{connectionId:guid}/password")]
    [EndpointSummary("Update connection password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePassword(
        Guid connectionId,
        [FromBody] UpdateConnectionPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<bool>();
        }

        var result = await _connectionManagementService.UpdatePasswordAsync(
            connectionId,
            userId,
            request.NewPassword,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Connection password updated.");
    }

    [HttpDelete("{connectionId:guid}")]
    [EndpointSummary("Delete a connection")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteConnection(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<bool>();
        }

        var result = await _connectionManagementService.DeleteConnectionAsync(connectionId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Connection deleted.");
    }

    [HttpPost("{connectionId:guid}/transfer-ownership")]
    [EndpointSummary("Transfer connection ownership")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> TransferOwnership(
        Guid connectionId,
        [FromBody] TransferConnectionOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<bool>();
        }

        var result = await _connectionManagementService.TransferOwnershipAsync(
            connectionId,
            userId,
            request.NewAdminEmail,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Connection ownership transferred.");
    }

    [HttpPost("{connectionId:guid}/health-test")]
    [EndpointSummary("Test health for an existing connection")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionTestResult>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ConnectionTestResult>>> TestExistingConnectionHealth(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<ConnectionTestResult>();
        }

        var userConnection = await _connectionManagementService.GetConnectionByIdAsync(
            connectionId,
            userId,
            cancellationToken);

        if (userConnection.IsFailure)
        {
            var failure = Result<ConnectionTestResult>.Failure(userConnection.Errors, userConnection.Message);
            return ToApiResponse(failure, StatusCodes.Status200OK, "Connection health test completed.");
        }

        if (userConnection.Value is null || !userConnection.Value.IsAdmin)
        {
            var failure = Result<ConnectionTestResult>.Failure(
                ConnectionErrors.AdminNotOwner(connectionId.ToString(), nameof(connectionId)));
            return ToApiResponse(failure, StatusCodes.Status200OK, "Connection health test completed.");
        }

        var result = await _connectionManagementService.TestExistingConnectionHealthAsync(connectionId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Connection health test completed.");
    }
}
