using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/schema")]
[Tags("Schema")]
public sealed class SchemaController(
    IConnectionManagementService connectionManagementService,
    ISchemaContextService schemaContextService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IConnectionManagementService _connectionManagementService = connectionManagementService;
    private readonly ISchemaContextService _schemaContextService = schemaContextService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("connections/{connectionId:guid}/refresh")]
    [EndpointSummary("Refresh schema snapshot for a connection (admin only)")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object?>>> RefreshSchema(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<object?>();
        }

        var connectionResult = await _connectionManagementService.GetConnectionByIdAsync(
            connectionId,
            userId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            var failure = Result.Failure(connectionResult.Errors, connectionResult.Message);
            return ToApiResponse(failure, StatusCodes.Status400BadRequest, "Connection load failed.");
        }

        if (connectionResult.Value is null || !connectionResult.Value.IsAdmin)
        {
            var failure = Result.Failure(
                new Error("schema.refresh_forbidden", "User is not authorized to refresh schema.", nameof(userId)));

            return ToApiResponse(failure, StatusCodes.Status200OK, "Schema refresh failed.");
        }

        try
        {
            await _schemaContextService.RefreshSchemaAsync(connectionId, userId, cancellationToken);
            return ToApiResponse(Result.Success(), StatusCodes.Status200OK, "Schema refresh completed.");
        }
        catch
        {
            var failure = Result.Failure(
                new Error("schema.refresh_failed", "Schema refresh failed."));
            return ToApiResponse(failure, StatusCodes.Status200OK, "Schema refresh failed.");
        }
    }
}
