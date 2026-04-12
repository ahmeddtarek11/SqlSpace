using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Controllers;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.ConnectionManagement.Dtos;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Tests.Api;

public sealed class ConnectionManagementControllerTests
{
    [Fact]
    public async Task CreateConnection_ShouldReturnUnauthorized_WhenCurrentUserIsMissing()
    {
        var service = new FakeConnectionManagementService();
        var currentUser = new FakeCurrentUserService(null);
        var controller = CreateController(service, currentUser);

        var request = new CreateConnectionRequest
        {
            ConnectionName = "primary",
            DatabaseProvider = DbProviders.PostgreSql,
            InputMode = ConnectionInputMode.RawConnectionString,
            RawConnectionString = "Host=db.local;Port=5432;Database=main;Username=db-user;Password=secret"
        };

        var actionResult = await controller.CreateConnection(request, CancellationToken.None);

        var objectResult = actionResult.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<ConnectionCreationResponse>>().Subject;
        response.Success.Should().BeFalse();
        service.CreateConnectionCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetUserConnections_ShouldWrapSuccessResult()
    {
        var service = new FakeConnectionManagementService
        {
            GetUserConnectionsResult = Result<IReadOnlyList<ConnectionSummaryDto>>.Success(
                [
                    new ConnectionSummaryDto
                    {
                        ConnectionId = Guid.NewGuid(),
                        ConnectionName = "analytics",
                        DatabaseProvider = DbProviders.PostgreSql,
                        IsAdmin = true,
                        HasFullAccess = true,
                        IsHealthy = true,
                        CreatedAt = DateTime.UtcNow,
                        ConnectionSummary = "{}"
                    }
                ])
        };
        var currentUser = new FakeCurrentUserService("user-1");
        var controller = CreateController(service, currentUser);

        var actionResult = await controller.GetUserConnections(CancellationToken.None);

        var objectResult = actionResult.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<IReadOnlyList<ConnectionSummaryDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(1);
        service.LastGetUserConnectionsUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetConnectionById_ShouldReturnBadRequest_WhenServiceFails()
    {
        var connectionId = Guid.NewGuid();
        var service = new FakeConnectionManagementService
        {
            GetConnectionByIdResult = Result<ConnectionDto?>.Failure(
                ConnectionErrors.ConnectionNotFound(connectionId.ToString(), nameof(connectionId)))
        };
        var currentUser = new FakeCurrentUserService("user-1");
        var controller = CreateController(service, currentUser);

        var actionResult = await controller.GetConnectionById(connectionId, CancellationToken.None);

        var objectResult = actionResult.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<ConnectionDto?>>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().ContainSingle(error => error.Code == ConnectionErrors.ConnectionNotFoundCode);
    }

    [Fact]
    public async Task TestExistingConnectionHealth_ShouldRejectNonAdmin_AndSkipHealthCheck()
    {
        var connectionId = Guid.NewGuid();
        var service = new FakeConnectionManagementService
        {
            GetConnectionByIdResult = Result<ConnectionDto?>.Success(
                new ConnectionDto
                {
                    ConnectionId = connectionId,
                    ConnectionName = "readonly",
                    DatabaseProvider = DbProviders.PostgreSql,
                    IsAdmin = false
                })
        };
        var currentUser = new FakeCurrentUserService("user-1");
        var controller = CreateController(service, currentUser);

        var actionResult = await controller.TestExistingConnectionHealth(connectionId, CancellationToken.None);

        var objectResult = actionResult.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<ConnectionTestResult>>().Subject;
        response.Success.Should().BeFalse();
        response.Errors.Should().ContainSingle(error => error.Code == ConnectionErrors.AdminNotOwnerCode);
        service.TestExistingConnectionHealthCalls.Should().Be(0);
    }

    [Fact]
    public async Task TestExistingConnectionHealth_ShouldCallService_WhenCurrentUserIsAdmin()
    {
        var connectionId = Guid.NewGuid();
        var service = new FakeConnectionManagementService
        {
            GetConnectionByIdResult = Result<ConnectionDto?>.Success(
                new ConnectionDto
                {
                    ConnectionId = connectionId,
                    ConnectionName = "main",
                    DatabaseProvider = DbProviders.PostgreSql,
                    IsAdmin = true
                }),
            TestExistingConnectionHealthResult = Result<ConnectionTestResult>.Success(
                new ConnectionTestResult
                {
                    Success = true,
                    DatabaseName = "main",
                    ResponseTimeMs = 5
                })
        };
        var currentUser = new FakeCurrentUserService("admin-1");
        var controller = CreateController(service, currentUser);

        var actionResult = await controller.TestExistingConnectionHealth(connectionId, CancellationToken.None);

        var objectResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = objectResult.Value.Should().BeOfType<ApiResponse<ConnectionTestResult>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        service.TestExistingConnectionHealthCalls.Should().Be(1);
    }

    private static ConnectionManagementController CreateController(
        IConnectionManagementService service,
        ICurrentUserService currentUserService)
    {
        var controller = new ConnectionManagementController(service, currentUserService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private sealed class FakeCurrentUserService(string? userId) : ICurrentUserService
    {
        private readonly string? _userId = userId;

        public string? GetUserId() => _userId;

        public string? GetUserEmail() => null;

        public string? GetUserName() => null;

        public bool IsAuthenticated() => !string.IsNullOrWhiteSpace(_userId);

        public IReadOnlyCollection<string> GetUserRoles() => Array.Empty<string>();

        public string? GetClientIpAddress() => null;
    }

    private sealed class FakeConnectionManagementService : IConnectionManagementService
    {
        public Result<ConnectionCreationResponse> CreateConnectionResult { get; set; } =
            Result<ConnectionCreationResponse>.Success(
                new ConnectionCreationResponse
                {
                    ConnectionId = Guid.NewGuid(),
                    ConnectionName = "new-connection",
                    Provider = DbProviders.PostgreSql,
                    AdminId = "admin-1",
                    ConnectionAdminName = "admin"
                });

        public Result<ConnectionTestResult> TestNewConnectionResult { get; set; } =
            Result<ConnectionTestResult>.Success(new ConnectionTestResult { Success = true, ResponseTimeMs = 7 });

        public Result<bool> UpdatePasswordResult { get; set; } = Result<bool>.Success(true);
        public Result<bool> DeleteConnectionResult { get; set; } = Result<bool>.Success(true);

        public Result<ConnectionDto?> GetConnectionByIdResult { get; set; } =
            Result<ConnectionDto?>.Success(
                new ConnectionDto
                {
                    ConnectionId = Guid.NewGuid(),
                    ConnectionName = "main",
                    DatabaseProvider = DbProviders.PostgreSql,
                    IsAdmin = true
                });

        public Result<IReadOnlyList<ConnectionSummaryDto>> GetUserConnectionsResult { get; set; } =
            Result<IReadOnlyList<ConnectionSummaryDto>>.Success(Array.Empty<ConnectionSummaryDto>());

        public Result<bool> TransferOwnershipResult { get; set; } = Result<bool>.Success(true);

        public Result<ConnectionTestResult> TestExistingConnectionHealthResult { get; set; } =
            Result<ConnectionTestResult>.Success(new ConnectionTestResult { Success = true, ResponseTimeMs = 4 });

        public int CreateConnectionCalls { get; private set; }
        public int TestExistingConnectionHealthCalls { get; private set; }
        public string? LastGetUserConnectionsUserId { get; private set; }

        public Task<Result<ConnectionCreationResponse>> CreateConnectionAsync(
            string userId,
            CreateConnectionRequest request,
            CancellationToken cancellationToken)
        {
            CreateConnectionCalls++;
            return Task.FromResult(CreateConnectionResult);
        }

        public Task<Result<ConnectionTestResult>> TestNewConnectionAsync(
            TestConnectionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(TestNewConnectionResult);
        }

        public Task<Result<bool>> UpdatePasswordAsync(
            Guid connectionId,
            string userId,
            string newPassword,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdatePasswordResult);
        }

        public Task<Result<bool>> DeleteConnectionAsync(
            Guid connectionId,
            string userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(DeleteConnectionResult);
        }

        public Task<Result<ConnectionDto?>> GetConnectionByIdAsync(
            Guid connectionId,
            string userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GetConnectionByIdResult);
        }

        public Task<Result<IReadOnlyList<ConnectionSummaryDto>>> GetUserConnectionsAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            LastGetUserConnectionsUserId = userId;
            return Task.FromResult(GetUserConnectionsResult);
        }

        public Task<Result<bool>> TransferOwnershipAsync(
            Guid connectionId,
            string currentAdminUserId,
            string newAdminEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(TransferOwnershipResult);
        }

        public Task<Result<ConnectionTestResult>> TestExistingConnectionHealthAsync(
            Guid connectionId,
            CancellationToken cancellationToken)
        {
            TestExistingConnectionHealthCalls++;
            return Task.FromResult(TestExistingConnectionHealthResult);
        }
    }
}
