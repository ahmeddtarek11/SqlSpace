using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICurrentUserService currentUserService) : ApiController
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegisterResult>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status201Created, "Registration completed.");
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthTokensResult>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Login completed.");
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokensResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthTokensResult>>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Token refreshed.");
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<object?>();
        }

        var result = await _authService.LogoutAsync(userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Logout completed.");
    }

    

    public sealed class RefreshRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

}
