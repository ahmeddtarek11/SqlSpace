using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiController
{
    private readonly IAuthService _authService = authService;

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
    public async Task<ActionResult<ApiResponse<object?>>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(request.UserId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Logout completed.");
    }

    

    public sealed class RefreshRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public sealed class LogoutRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}
