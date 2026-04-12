using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Controllers.AccessControl.Dtos;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Auth;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Route("api/AccessControl")]
[Tags("Access Control")]
public partial class AccessControlController(
    IAccessControlService accessControlService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IAccessControlService _accessControlService = accessControlService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("connections/{connectionId:guid}/grants")]
    [EndpointSummary("Grant access to a user on a connection")]
    [EndpointDescription("Parameters: connectionId (path) target connection id, request (body) target user email + access mode + optional restricted tables, userId (query optional) acting admin id. userId fallback: if omitted, current authenticated user id from JWT claims is used.")]
    [ProducesResponseType(typeof(ApiResponse<UserAccessSummary>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserAccessSummary>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserAccessSummary>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserAccessSummary>>> GrantAccess(
        Guid connectionId,
        [FromBody] GrantAccessRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return  UnauthorizedResponse<UserAccessSummary>();
        }

        var result = await _accessControlService.GrantAccessAsync(
            connectionId,
            adminUserId,
            request.TargetUserEmail,
            request.HasFullAccess,
            request.RestrictedTables,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status201Created, "Access granted.");
    }

    [HttpPut("connections/{connectionId:guid}/users/{targetUserId}/restrictions")]
    [EndpointSummary("Update existing access restrictions for a user")]
    [EndpointDescription("Parameters: connectionId (path) target connection id, targetUserId (path) user being updated, request (body) full-access flag + optional restricted tables, userId (query optional) acting admin id. userId fallback: if omitted, current authenticated user id from JWT claims is used.")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateAccessRestrictions(
        Guid connectionId,
        string targetUserId,
        [FromBody] UpdateAccessRestrictionsRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return UnauthorizedResponse<object?>();
        }

        var result = await _accessControlService.UpdateAccessRestrictionsAsync(
            connectionId,
            adminUserId,
            targetUserId,
            request.HasFullAccess,
            request.RestrictedTables,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Access restrictions updated.");
    }

    [HttpDelete("connections/{connectionId:guid}/users/{targetUserId}")]
    [EndpointSummary("Revoke a user's access from a connection")]
    [EndpointDescription("Parameters: connectionId (path) target connection id, targetUserId (path) user to revoke, userId (query optional) acting admin id. userId fallback: if omitted, current authenticated user id from JWT claims is used.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeAccess(
        Guid connectionId,
        string targetUserId,
        CancellationToken cancellationToken)
    {
        var adminUserId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return UnauthorizedResponse<bool>();
        }

        var result = await _accessControlService.RevokeAccessAsync(
            connectionId,
            adminUserId,
            targetUserId,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Access revoked.");
    }

    [HttpGet("connections/{connectionId:guid}/users")]
    [EndpointSummary("List users who have access to the connection")]
    [ProducesResponseType(typeof(ApiResponse<ICollection<UserAccessSummary>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ICollection<UserAccessSummary>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ICollection<UserAccessSummary>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ICollection<UserAccessSummary>>>> ListConnectionUsers(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var adminUserId = _currentUserService.GetUserId();;
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return UnauthorizedResponse<ICollection<UserAccessSummary>>();
        }

        var result = await _accessControlService.ListConnectionUsersAsync(
            connectionId,
            adminUserId,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Connection users loaded.");
    }

    [HttpGet("connections/{connectionId:guid}/has-access")]
    [EndpointSummary("Check if a user has access to the connection")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> HasAccessToConnection(
        Guid connectionId,
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
       

        var result = await _accessControlService.HasAccessToConnectionAsync(
            connectionId,
            userId,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, $"Connection access evaluated -- Has Access : {result.Value}.");
    }

    [HttpGet("connections/{connectionId:guid}/can-access-table")]
    [EndpointSummary("Check if a user can access a specific table -  null schema name only for mysql")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> CanAccessTable(
        Guid connectionId,
        [FromQuery] TableAccessCheckRequest request,
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
    

        var result = await _accessControlService.CanAccessTableAsync(
            connectionId,
            userId,
            request.TableName,
            request.SchemaName,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Table access evaluated.");
    }

    [HttpGet("connections/{connectionId:guid}/accessible-tables")]
    [EndpointSummary("Get all table names accessible by a user")]
    [EndpointDescription("Parameters: connectionId (path) target connection id, userId (query optional) user id to evaluate. userId fallback: if omitted, current authenticated user id from JWT claims is used.")]
    [ProducesResponseType(typeof(ApiResponse<ICollection<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ICollection<string>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ICollection<string>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ICollection<string>>>> GetAccessibleTableNames(
        Guid connectionId,
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
       

        var result = await _accessControlService.GetAccessibleTableNamesAsync(
            connectionId,
            userId,
            cancellationToken);

        return ToApiResponse(result, StatusCodes.Status200OK, "Accessible tables loaded.");
    }


     [EndpointSummary("check if a user is the admin of a connection")]
    [EndpointDescription("Parameters: connectionId (path) target connection id, userId (query optional) user id to evaluate. userId fallback: if omitted, current authenticated user id from JWT claims is used.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
     [HttpGet("connections/IsAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> isAdmin(Guid ConnectionId)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<bool>();
        }
        var res = await _accessControlService.IsAdmin(ConnectionId, userId);
        return ToApiResponse(res, StatusCodes.Status200OK, "Success");
    }
    
}
