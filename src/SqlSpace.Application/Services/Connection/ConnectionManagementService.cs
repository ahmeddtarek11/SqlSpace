using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SqlSpace.Application.Abstractions.Audit;
using SqlSpace.Application.Abstractions.ConnectionManagement.Dtos;
using SqlSpace.Application.Abstractions.Connections;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using System.Text.Json;

namespace SqlSpace.Application.Services.Connection;

public class ConnectionManagementService(
    IDatabaseExecutor databaseExecutor,
    IConnectionStringBuilder connectionStringBuilder,
    IEncryptionService encryptionService,
    IApplicationDbContext dbContext,
    IUserRepository userRepo,
    IAuditLogRepository auditLog,
    ILogger<ConnectionManagementService> logger) : IConnectionManagementService
{
    private readonly IDatabaseExecutor _databaseExecutor = databaseExecutor;
    private readonly IConnectionStringBuilder _connectionStringBuilder = connectionStringBuilder;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IAuditLogRepository _auditLog = auditLog;
    private readonly ILogger<ConnectionManagementService> _logger = logger;


    public async Task<Result<ConnectionCreationResponse>> CreateConnectionAsync(string userId, CreateConnectionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CreateConnection requested. UserId: {UserId}, ConnectionName: {ConnectionName}, Provider: {Provider}, InputMode: {InputMode}",
            userId,
            request?.ConnectionName,
            request?.DatabaseProvider,
            request?.InputMode);

        if (request is null)
        {
        _logger.LogWarning("CreateConnection failed validation: request is null. UserId: {UserId}", userId);
        return ConnectionErrors.InvalidRequest(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("CreateConnection failed validation: userId is empty.");
           return ConnectionErrors.UserIdRequired(nameof(userId));
        }

        if (request.InputMode != ConnectionInputMode.RawConnectionString &&
              request.InputMode != ConnectionInputMode.IndividualFields)
        {
            _logger.LogWarning(
                "CreateConnection failed validation: unsupported input mode {InputMode}. UserId: {UserId}",
                request.InputMode,
                userId);
            return ConnectionErrors.UnsupportedInputMode(request.InputMode.ToString(), nameof(request.InputMode));
        }



        var user = await  _userRepo.GetByIdAsync(userId, cancellationToken);

        if(user is null)
        {
            _logger.LogWarning("CreateConnection failed: user not found. UserId: {UserId}", userId);
            return ConnectionErrors.InvalidUserId(userId);
        }

        _logger.LogDebug("CreateConnection user validated. UserId: {UserId}", userId);

        var newConnection = new TestConnectionRequest
        {
             DatabaseProvider = request.DatabaseProvider ,
             DatabaseName = request.DatabaseName ,
             Host = request.Host,
             InputMode =  request.InputMode,
             Port = request.Port ,
             Username = request.Username,
             Password =  request.Password,
             UseSSL =  request.UseSSL,
             AdditionalParameters =  request.AdditionalParameters,
             RawConnectionString = request.RawConnectionString
             
        };
        
        _logger.LogInformation(
            "Running connection test before create. UserId: {UserId}, Provider: {Provider}, InputMode: {InputMode}",
            userId,
            request.DatabaseProvider,
            request.InputMode);
        var testRes =  await TestNewConnectionAsync(newConnection , cancellationToken);

        if(testRes.IsFailure)
        {
            var testErrorMessage = testRes.Message;
            if (string.IsNullOrWhiteSpace(testErrorMessage) && testRes.Errors.Count > 0)
            {
                testErrorMessage = testRes.Errors[0].Message;
            }

            _logger.LogWarning(
                "CreateConnection aborted: test failed. UserId: {UserId}, Provider: {Provider}, InputMode: {InputMode}, Error: {ErrorMessage}",
                userId,
                request.DatabaseProvider,
                request.InputMode,
                testErrorMessage);
           return Result<ConnectionCreationResponse>.Failure(
               testRes.Errors,
               testErrorMessage ?? "Connection test failed.");
        }

        _logger.LogInformation("Connection test succeeded. UserId: {UserId}, ConnectionName: {ConnectionName}", userId, request.ConnectionName);

        var TestedConnection = new ConnectedDatabase
        {
            ConnectionId = Guid.NewGuid(),
            CreatedByUserId = userId,
            DbAdminId = userId,
            ConnectionName = request.ConnectionName,
            CreatedAt = DateTime.UtcNow,
            DatabaseProvider = request.DatabaseProvider,
            UseSSL = request.UseSSL,
            LastSuccessfulConnection = DateTime.UtcNow,
            IsHealthy = true,
        };

        if(request.InputMode == ConnectionInputMode.RawConnectionString)
        {
            _logger.LogDebug(
                "Processing raw connection string mode for create. UserId: {UserId}, ConnectionName: {ConnectionName}",
                userId,
                request.ConnectionName);
            TestedConnection.UsesRawConnectionString =true;
            TestedConnection.EncryptedRawConnectionString = _encryptionService.Encrypt(request.RawConnectionString!.Trim());
        }
        else if(request.InputMode == ConnectionInputMode.IndividualFields)
        {
            _logger.LogDebug(
                "Processing individual fields mode for create. UserId: {UserId}, ConnectionName: {ConnectionName}",
                userId,
                request.ConnectionName);
            var port = request.Port ?? request.DatabaseProvider.GetDefaultPort();


            TestedConnection.DatabaseName = testRes.Value!.ExtractedComponents!.DatabaseName;
            TestedConnection.Host = testRes.Value.ExtractedComponents.Host;
            TestedConnection.PortNumber = testRes.Value.ExtractedComponents.Port;
            TestedConnection.Username = testRes.Value.ExtractedComponents.Username;
            TestedConnection.EncryptedPassword = _encryptionService.Encrypt(request.Password!.Trim());
            TestedConnection.AdditionalParameters = testRes.Value.ExtractedComponents.AdditionalParameters;
            TestedConnection.UsesRawConnectionString =false;

            var rawConnectionString = _connectionStringBuilder.BuildConnectionString(
                request.DatabaseProvider,TestedConnection.Host,port,TestedConnection.DatabaseName
                ,TestedConnection.Username,request.Password,request.UseSSL,request.AdditionalParameters);

            TestedConnection.EncryptedRawConnectionString = _encryptionService.Encrypt(rawConnectionString);
            
        }

        await _dbContext.ConnectedDatabases.AddAsync(TestedConnection , cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Connection created successfully. ConnectionId: {ConnectionId}, UserId: {UserId}, Provider: {Provider}",
            TestedConnection.ConnectionId,
            userId,
            TestedConnection.DatabaseProvider);

        return new ConnectionCreationResponse
        {
            ConnectionName =  TestedConnection.ConnectionName,
            ConnectionId = TestedConnection.ConnectionId,
            Provider = TestedConnection.DatabaseProvider,
            AdminId = userId,
            ConnectionAdminName = user!.UserName,
            DatabaseName = TestedConnection.DatabaseName,
            UseSSL = TestedConnection.UseSSL,
            CreatedAt = TestedConnection.CreatedAt


        };

        
    }

    public async Task<Result<bool>> DeleteConnectionAsync(Guid connectionId, string userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteConnection requested. ConnectionId: {ConnectionId}, UserId: {UserId}",
            connectionId,
            userId);

        if (connectionId == Guid.Empty)
        {
            _logger.LogWarning("DeleteConnection failed validation: empty connection id. UserId: {UserId}", userId);
            return ConnectionErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("DeleteConnection failed validation: empty user id. ConnectionId: {ConnectionId}", connectionId);
            return ConnectionErrors.UserIdRequired(nameof(userId));
        }

        try
        {
            var connection = await _dbContext.ConnectedDatabases
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == connectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                _logger.LogWarning(
                    "DeleteConnection failed: connection not found or already deleted. ConnectionId: {ConnectionId}, UserId: {UserId}",
                    connectionId,
                    userId);
                return ConnectionErrors.ConnectionNotFound(connectionId.ToString(), nameof(connectionId));
            }

            if (!string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "DeleteConnection denied: user is not owner. ConnectionId: {ConnectionId}, OwnerId: {OwnerId}, UserId: {UserId}",
                    connectionId,
                    connection.DbAdminId,
                    userId);
                return ConnectionErrors.AdminNotOwner(connectionId.ToString(), nameof(userId));
            }

            var now = DateTime.UtcNow;

            connection.IsDeleted = true;
            connection.DeletedAt = now;
            connection.DeletedByUserId = userId;

            var activeAccesses = await _dbContext.UserDatabaseAccesses
                .Where(a => a.DatabaseConnectionId == connectionId && !a.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var access in activeAccesses)
            {
                access.IsDeleted = true;
                access.RevokedAt ??= now;
                access.RevokedByUserId ??= userId;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "DeleteConnection succeeded. ConnectionId: {ConnectionId}, UserId: {UserId}, SoftDeletedAccesses: {AccessCount}",
                connectionId,
                userId,
                activeAccesses.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteConnection failed unexpectedly. ConnectionId: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
            return ConnectionErrors.DeleteConnectionFailed(
                "Failed to delete connection due to an unexpected error.",
                nameof(connectionId));
        }
    }

    public async Task<Result<ConnectionDto?>> GetConnectionByIdAsync(Guid connectionId, string userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetConnectionById requested. ConnectionId: {ConnectionId}, UserId: {UserId}",
            connectionId,
            userId);

        if (connectionId == Guid.Empty)
        {
            _logger.LogWarning("GetConnectionById failed validation: empty connection id. UserId: {UserId}", userId);
            return ConnectionErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetConnectionById failed validation: empty user id. ConnectionId: {ConnectionId}", connectionId);
            return ConnectionErrors.UserIdRequired(nameof(userId));
        }

        try
        {
            var connection = await _dbContext.ConnectedDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == connectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                _logger.LogWarning(
                    "GetConnectionById failed: connection not found. ConnectionId: {ConnectionId}, UserId: {UserId}",
                    connectionId,
                    userId);
                return ConnectionErrors.ConnectionNotFound(connectionId.ToString(), nameof(connectionId));
            }

            var isAdmin = string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal);

            var hasAccess = isAdmin || await _dbContext.UserDatabaseAccesses
                .AsNoTracking()
                .AnyAsync(
                    a => a.DatabaseConnectionId == connectionId
                        && a.UserId == userId
                        && !a.IsDeleted
                        && a.RevokedAt == null,
                    cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "GetConnectionById denied: user has no access. ConnectionId: {ConnectionId}, UserId: {UserId}",
                    connectionId,
                    userId);
                return ConnectionErrors.AdminNotOwner(connectionId.ToString(), nameof(userId));
            }

            var summary = connection.UsesRawConnectionString
                ? $"{connection.DatabaseProvider} (raw)"
                : $"{connection.DatabaseProvider} {connection.Host}:{connection.PortNumber}/{connection.DatabaseName}";

            var dto = new ConnectionDto
            {
                ConnectionId = connection.ConnectionId,
                ConnectionName = connection.ConnectionName,
                DatabaseProvider = connection.DatabaseProvider,
                Host = connection.Host,
                Port = connection.PortNumber,
                DatabaseName = connection.DatabaseName,
                Username = connection.Username,
                UseSSL = connection.UseSSL,
                UsesRawConnectionString = connection.UsesRawConnectionString,
                IsHealthy = connection.IsHealthy,
                LastSuccessfulConnection = connection.LastSuccessfulConnection,
                LastConnectionError = null,
                CreatedAt = connection.CreatedAt,
                IsAdmin = isAdmin,
                ConnectionSummary = summary
            };

            _logger.LogInformation(
                "GetConnectionById succeeded. ConnectionId: {ConnectionId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
                connectionId,
                userId,
                isAdmin);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "GetConnectionById failed unexpectedly. ConnectionId: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);

            return ConnectionErrors.Unexpected(
                "Failed to retrieve connection details due to an unexpected error.",
                nameof(connectionId));
        }
    }

   public async Task<Result<IReadOnlyList<ConnectionSummaryDto>>> GetUserConnectionsAsync(string userId,CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(userId))
        return ConnectionErrors.UserIdRequired();

    var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
    if (user is null)
        return ConnectionErrors.InvalidUserId(userId);

    var rows = await _dbContext.ConnectedDatabases
        .AsNoTracking()
        .Where(c => !c.IsDeleted &&
            (c.DbAdminId == userId ||
             c.UserAccesses.Any(a =>
                 a.UserId == userId &&
                 !a.IsDeleted &&
                 a.RevokedAt == null)))
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new
        {
            Connection = c,
            IsAdmin = c.DbAdminId == userId,
            HasFullAccess = c.DbAdminId == userId
                ? true
                : c.UserAccesses
                    .Where(a => a.UserId == userId && !a.IsDeleted && a.RevokedAt == null)
                    .Select(a => (bool?)a.HasFullAccess)
                    .FirstOrDefault() ?? false
        })
        .ToListAsync(cancellationToken);

    var result = rows.Select(x => new ConnectionSummaryDto
    {
        ConnectionId = x.Connection.ConnectionId,
        ConnectionName = x.Connection.ConnectionName,
        DatabaseProvider = x.Connection.DatabaseProvider,
        IsHealthy = x.Connection.IsHealthy,
        CreatedAt = x.Connection.CreatedAt,
        IsAdmin = x.IsAdmin,
        HasFullAccess = x.HasFullAccess,
        ConnectionSummary = x.IsAdmin ? ConnectionSummaryHelper(x.Connection) : string.Empty
    }).ToList();

    return result;
}


    public async Task<Result<bool>> TransferOwnershipAsync(Guid connectionId, string currentAdminUserId, string newAdminEmail, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "TransferOwnership requested. ConnectionId: {ConnectionId}, CurrentAdminUserId: {CurrentAdminUserId}, NewAdminEmail: {NewAdminEmail}",
            connectionId,
            currentAdminUserId,
            newAdminEmail);

        if (connectionId == Guid.Empty)
        {
            _logger.LogWarning("TransferOwnership failed validation: empty connection id. CurrentAdminUserId: {CurrentAdminUserId}", currentAdminUserId);
            return ConnectionErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(currentAdminUserId))
        {
            _logger.LogWarning("TransferOwnership failed validation: empty current admin user id. ConnectionId: {ConnectionId}", connectionId);
            return ConnectionErrors.UserIdRequired(nameof(currentAdminUserId));
        }

        if (string.IsNullOrWhiteSpace(newAdminEmail))
        {
            _logger.LogWarning("TransferOwnership failed validation: empty new admin email. ConnectionId: {ConnectionId}, CurrentAdminUserId: {CurrentAdminUserId}", connectionId, currentAdminUserId);
            return ConnectionErrors.InvalidRequest(nameof(newAdminEmail));
        }

        var normalizedNewAdminEmail = newAdminEmail.Trim();

        try
        {
            var connection = await _dbContext.ConnectedDatabases
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == connectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                _logger.LogWarning(
                    "TransferOwnership failed: connection not found. ConnectionId: {ConnectionId}, CurrentAdminUserId: {CurrentAdminUserId}",
                    connectionId,
                    currentAdminUserId);
                return ConnectionErrors.ConnectionNotFound(connectionId.ToString(), nameof(connectionId));
            }

            if (!string.Equals(connection.DbAdminId, currentAdminUserId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "TransferOwnership denied: current user is not owner. ConnectionId: {ConnectionId}, OwnerId: {OwnerId}, CurrentAdminUserId: {CurrentAdminUserId}",
                    connectionId,
                    connection.DbAdminId,
                    currentAdminUserId);
                return ConnectionErrors.AdminNotOwner(connectionId.ToString(), nameof(currentAdminUserId));
            }

            var newAdmin = await _userRepo.GetByEmailAsync(normalizedNewAdminEmail, cancellationToken);
            if (newAdmin is null)
            {
                _logger.LogWarning(
                    "TransferOwnership failed: new admin user not found by email. ConnectionId: {ConnectionId}, NewAdminEmail: {NewAdminEmail}",
                    connectionId,
                    normalizedNewAdminEmail);
                return ConnectionErrors.InvalidUserId(normalizedNewAdminEmail, nameof(newAdminEmail));
            }

            if (string.Equals(newAdmin.Id, currentAdminUserId, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "TransferOwnership no-op: target admin is already current owner. ConnectionId: {ConnectionId}, UserId: {UserId}",
                    connectionId,
                    currentAdminUserId);
                return true;
            }

            var now = DateTime.UtcNow;
            var previousAdminUserId = connection.DbAdminId;
            connection.DbAdminId = newAdmin.Id;

            var oldAdminAccess = await _dbContext.UserDatabaseAccesses
                .FirstOrDefaultAsync(
                    a => a.DatabaseConnectionId == connectionId && a.UserId == currentAdminUserId,
                    cancellationToken);

            if (oldAdminAccess is null)
            {
                await _dbContext.UserDatabaseAccesses.AddAsync(
                    new UserDatabaseAccess
                    {
                        Id = Guid.NewGuid(),
                        UserId = currentAdminUserId,
                        DatabaseConnectionId = connectionId,
                        HasFullAccess = true,
                        RestrictedTablesJson = null,
                        GrantedAt = now,
                        GrantedByUserId = currentAdminUserId,
                        IsDeleted = false,
                        RevokedAt = null,
                        RevokedByUserId = null
                    },
                    cancellationToken);
            }
            else
            {
                oldAdminAccess.IsDeleted = false;
                oldAdminAccess.RevokedAt = null;
                oldAdminAccess.RevokedByUserId = null;
                oldAdminAccess.HasFullAccess = true;
                oldAdminAccess.RestrictedTablesJson = null;
                oldAdminAccess.GrantedAt = now;
                oldAdminAccess.GrantedByUserId = currentAdminUserId;
            }

            var targetAdminAccesses = await _dbContext.UserDatabaseAccesses
                .Where(a => a.DatabaseConnectionId == connectionId && a.UserId == newAdmin.Id && !a.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var access in targetAdminAccesses)
            {
                access.IsDeleted = true;
                access.RevokedAt ??= now;
                access.RevokedByUserId ??= currentAdminUserId;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var auditResult = await _auditLog.LogOwnershipTransferAsync(
                connectionId,
                previousAdminUserId,
                newAdmin.Id,
                cancellationToken);

            if (auditResult.IsFailure)
            {
                _logger.LogWarning(
                    "TransferOwnership succeeded but audit logging failed. ConnectionId: {ConnectionId}, PreviousAdminUserId: {PreviousAdminUserId}, NewAdminUserId: {NewAdminUserId}",
                    connectionId,
                    previousAdminUserId,
                    newAdmin.Id);
            }

            _logger.LogInformation(
                "TransferOwnership succeeded. ConnectionId: {ConnectionId}, PreviousAdminUserId: {PreviousAdminUserId}, NewAdminUserId: {NewAdminUserId}",
                connectionId,
                previousAdminUserId,
                newAdmin.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TransferOwnership failed unexpectedly. ConnectionId: {ConnectionId}, CurrentAdminUserId: {CurrentAdminUserId}, NewAdminEmail: {NewAdminEmail}",
                connectionId,
                currentAdminUserId,
                normalizedNewAdminEmail);
            return ConnectionErrors.Unexpected(
                "Failed to transfer connection ownership due to an unexpected error.",
                nameof(connectionId));
        }
    }

    public async Task<Result<bool>> UpdatePasswordAsync(Guid connectionId, string userId, string newPassword, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdatePassword requested. ConnectionId: {ConnectionId}, UserId: {UserId}",
            connectionId,
            userId);

        if (connectionId == Guid.Empty)
        {
            _logger.LogWarning("UpdatePassword failed validation: empty connection id. UserId: {UserId}", userId);
            return ConnectionErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("UpdatePassword failed validation: empty user id. ConnectionId: {ConnectionId}", connectionId);
            return ConnectionErrors.UserIdRequired(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            _logger.LogWarning("UpdatePassword failed validation: empty password. ConnectionId: {ConnectionId}, UserId: {UserId}", connectionId, userId);
            return ConnectionErrors.PasswordRequired(nameof(newPassword));
        }

        var trimmedPassword = newPassword.Trim();

        try
        {
            var connection = await _dbContext.ConnectedDatabases
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == connectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                _logger.LogWarning(
                    "UpdatePassword failed: connection not found. ConnectionId: {ConnectionId}, UserId: {UserId}",
                    connectionId,
                    userId);
                return ConnectionErrors.ConnectionNotFound(connectionId.ToString(), nameof(connectionId));
            }

            if (!string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "UpdatePassword denied: user is not owner. ConnectionId: {ConnectionId}, OwnerId: {OwnerId}, UserId: {UserId}",
                    connectionId,
                    connection.DbAdminId,
                    userId);
                return ConnectionErrors.AdminNotOwner(connectionId.ToString(), nameof(userId));
            }

            var encryptedPassword = _encryptionService.Encrypt(trimmedPassword);

            var testConnection = new ConnectedDatabase
            {
                ConnectionId = connection.ConnectionId,
                CreatedByUserId = connection.CreatedByUserId,
                DbAdminId = connection.DbAdminId,
                ConnectionName = connection.ConnectionName,
                DatabaseProvider = connection.DatabaseProvider,
                DatabaseName = connection.DatabaseName,
                Host = connection.Host,
                PortNumber = connection.PortNumber,
                Username = connection.Username,
                AdditionalParameters = connection.AdditionalParameters,
                UseSSL = connection.UseSSL,
                UsesRawConnectionString = connection.UsesRawConnectionString
            };

            string? encryptedUpdatedRawConnectionString = null;

            if (connection.UsesRawConnectionString)
            {
                if (string.IsNullOrWhiteSpace(connection.EncryptedRawConnectionString))
                {
                    _logger.LogWarning(
                        "UpdatePassword failed: raw mode connection has no stored raw connection string. ConnectionId: {ConnectionId}",
                        connectionId);
                    return ConnectionErrors.InvalidConnectionString(
                        "Stored raw connection string is missing for this connection.",
                        nameof(connection.EncryptedRawConnectionString));
                }

                var decryptedRawConnectionString = _encryptionService.Decrypt(connection.EncryptedRawConnectionString);
                var validation = _connectionStringBuilder.ValidateConnectionString(
                    decryptedRawConnectionString,
                    connection.DatabaseProvider);

                if (!validation.IsValid || validation.ParsedComponents is null)
                {
                    _logger.LogWarning(
                        "UpdatePassword failed: could not parse existing raw connection string. ConnectionId: {ConnectionId}, Error: {ErrorMessage}",
                        connectionId,
                        validation.ErrorMessage);
                    return ConnectionErrors.InvalidConnectionString(
                        validation.ErrorMessage ?? "Stored raw connection string is invalid.",
                        nameof(connection.EncryptedRawConnectionString));
                }

                var parsed = validation.ParsedComponents;
                var rebuiltRawConnectionString = _connectionStringBuilder.BuildConnectionString(
                    connection.DatabaseProvider,
                    parsed.Host,
                    parsed.Port,
                    parsed.DatabaseName,
                    parsed.Username,
                    trimmedPassword,
                    parsed.UseSSL,
                    parsed.AdditionalParameters);

                encryptedUpdatedRawConnectionString = _encryptionService.Encrypt(rebuiltRawConnectionString);
                testConnection.EncryptedRawConnectionString = encryptedUpdatedRawConnectionString;
            }
            else
            {
                testConnection.EncryptedPassword = encryptedPassword;
            }

            var testResult = await _databaseExecutor.TestConnectionAsync(testConnection, cancellationToken);
            if (!testResult.Success)
            {
                _logger.LogWarning(
                    "UpdatePassword failed: connection test with new password failed. ConnectionId: {ConnectionId}, UserId: {UserId}, Error: {ErrorMessage}",
                    connectionId,
                    userId,
                    testResult.ErrorMessage);

                return ConnectionErrors.ConnectionTestFailed(
                    testResult.ErrorMessage ?? "Connection test failed with the new password.",
                    nameof(newPassword));
            }

            
            if (connection.UsesRawConnectionString)
            {
                connection.EncryptedRawConnectionString = encryptedUpdatedRawConnectionString!;
                connection.EncryptedPassword = null;
            }
            else
            {
                var port = connection.PortNumber ?? connection.DatabaseProvider.GetDefaultPort();
                var updatedRawConnectionString = _connectionStringBuilder.BuildConnectionString(
                    connection.DatabaseProvider,
                    connection.Host!,
                    port,
                    connection.DatabaseName,
                    connection.Username!,
                    trimmedPassword,
                    connection.UseSSL,
                    connection.AdditionalParameters);

                connection.EncryptedRawConnectionString = _encryptionService.Encrypt(updatedRawConnectionString);
                connection.EncryptedPassword = encryptedPassword;
            }

            connection.LastSuccessfulConnection = DateTime.UtcNow;
            connection.IsHealthy = true;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "UpdatePassword succeeded. ConnectionId: {ConnectionId}, UserId: {UserId}, ResponseTimeMs: {ResponseTimeMs}",
                connectionId,
                userId,
                testResult.ResponseTimeMs);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UpdatePassword failed unexpectedly. ConnectionId: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
            return ConnectionErrors.Unexpected(
                "Failed to update connection password due to an unexpected error.",
                nameof(connectionId));
        }
    }

    public async Task<Result<ConnectionTestResult>> TestNewConnectionAsync(
        TestConnectionRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ConnectionErrors.InvalidRequest(nameof(request));
        }

        if (!Enum.IsDefined(typeof(DbProviders), request.DatabaseProvider))
        {
            return ConnectionErrors.UnsupportedProvider(
                request.DatabaseProvider.ToString(),
                nameof(request.DatabaseProvider));
        }

        if (request.InputMode == ConnectionInputMode.IndividualFields)
        {
            var validationError = ValidateIndividualFieldsRequest(request);
            if (validationError is not null)
            {
                return validationError;
            }

            var connection = new ConnectedDatabase
            {
                ConnectionId = Guid.NewGuid(),
                CreatedByUserId = "connection-test-user",
                DbAdminId = "connection-test-user",
                ConnectionName = "Connection Test",
                DatabaseProvider = request.DatabaseProvider,
                Host = request.Host!.Trim(),
                PortNumber = request.Port ?? request.DatabaseProvider.GetDefaultPort(),
                DatabaseName = request.DatabaseName!.Trim(),
                Username = request.Username!.Trim(),
                EncryptedPassword = _encryptionService.Encrypt(request.Password!),
                UseSSL = request.UseSSL,
                AdditionalParameters = string.IsNullOrWhiteSpace(request.AdditionalParameters) ? null : request.AdditionalParameters.Trim(),
                UsesRawConnectionString = false
            };

            var componentModeResult = await _databaseExecutor.TestConnectionAsync(connection, cancellationToken);
            if (!componentModeResult.Success)
            {
                return ConnectionErrors.ConnectionTestFailed(
                    componentModeResult.ErrorMessage ?? "Connection test failed.",
                    nameof(request));
            }

            componentModeResult.ExtractedComponents = new ConnectionComponents
            {
                Host = connection.Host,
                Port = connection.PortNumber!.Value,
                DatabaseName = componentModeResult.DatabaseName ?? connection.DatabaseName,
                Username = connection.Username!,
                UseSSL = connection.UseSSL,
                Password = string.Empty,
                AdditionalParameters = connection.AdditionalParameters
            };

            return componentModeResult;
        }

        if (request.InputMode != ConnectionInputMode.RawConnectionString)
        {
            return ConnectionErrors.UnsupportedInputMode(
                request.InputMode.ToString(),
                nameof(request.InputMode));
        }

        if (string.IsNullOrWhiteSpace(request.RawConnectionString))
        {
            return ConnectionErrors.RawConnectionStringRequired(nameof(request.RawConnectionString));
        }

        var rawConnectionString = request.RawConnectionString.Trim();
        var validation = _connectionStringBuilder.ValidateConnectionString(
            rawConnectionString,
            request.DatabaseProvider);

        if (!validation.IsValid)
        {
            return ConnectionErrors.InvalidConnectionString(
                validation.ErrorMessage ?? "Invalid connection string format.",
                nameof(request.RawConnectionString));
        }

        var parsedComponents = validation.ParsedComponents;
        if (parsedComponents is null)
        {
            return ConnectionErrors.InvalidConnectionString(
                "Connection string validation succeeded without parsed components.",
                nameof(request.RawConnectionString));
        }

        var rawConnection = new ConnectedDatabase
        {
            ConnectionId = Guid.NewGuid(),
            CreatedByUserId = "connection-test-user",
            DbAdminId = "connection-test-user",
            ConnectionName = "Connection Test",
            DatabaseProvider = request.DatabaseProvider,
            UsesRawConnectionString = true,
            EncryptedRawConnectionString = _encryptionService.Encrypt(rawConnectionString),
            Host = parsedComponents.Host,
            PortNumber = parsedComponents.Port,
            DatabaseName = parsedComponents.DatabaseName,
            Username = parsedComponents.Username,
            UseSSL = parsedComponents.UseSSL,
            AdditionalParameters = parsedComponents.AdditionalParameters
        };

        var rawModeResult = await _databaseExecutor.TestConnectionAsync(rawConnection, cancellationToken);
        if (!rawModeResult.Success)
        {
            return ConnectionErrors.ConnectionTestFailed(
                rawModeResult.ErrorMessage ?? "Connection test failed.",
                nameof(request));
        }

        rawModeResult.ExtractedComponents = new ConnectionComponents
        {
            Host = parsedComponents.Host,
            Port = parsedComponents.Port,
            DatabaseName = parsedComponents.DatabaseName,
            Username = parsedComponents.Username,
            UseSSL = parsedComponents.UseSSL,
            Password = string.Empty,
            AdditionalParameters = parsedComponents.AdditionalParameters
        };
        return rawModeResult;
    }

    public async Task<Result<ConnectionTestResult>> TestExistingConnectionHealthAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "TestExistingConnectionHealth requested. ConnectionId: {ConnectionId}",
            connectionId);

        if (connectionId == Guid.Empty)
        {
            _logger.LogWarning("TestExistingConnectionHealth failed validation: empty connection id.");
            return ConnectionErrors.InvalidConnectionId(nameof(connectionId));
        }

        try
        {
            var connection = await _dbContext.ConnectedDatabases
                .FirstOrDefaultAsync(
                    c => c.ConnectionId == connectionId && !c.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                _logger.LogWarning(
                    "TestExistingConnectionHealth failed: connection not found. ConnectionId: {ConnectionId}",
                    connectionId);
                return ConnectionErrors.ConnectionNotFound(connectionId.ToString(), nameof(connectionId));
            }

            var testResult = await _databaseExecutor.TestConnectionAsync(connection, cancellationToken);
            if (!testResult.Success)
            {
                connection.IsHealthy = false;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "TestExistingConnectionHealth failed for connection. ConnectionId: {ConnectionId}, Error: {ErrorMessage}",
                    connectionId,
                    testResult.ErrorMessage);

                return ConnectionErrors.ConnectionTestFailed(
                    testResult.ErrorMessage ?? "Connection health test failed.",
                    nameof(connectionId));
            }

            connection.IsHealthy = true;
            connection.LastSuccessfulConnection = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TestExistingConnectionHealth succeeded. ConnectionId: {ConnectionId}, ResponseTimeMs: {ResponseTimeMs}",
                connectionId,
                testResult.ResponseTimeMs);

            return testResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TestExistingConnectionHealth failed unexpectedly. ConnectionId: {ConnectionId}",
                connectionId);
            return ConnectionErrors.Unexpected(
                "Failed to test connection health due to an unexpected error.",
                nameof(connectionId));
        }
    }




































    private static Error? ValidateIndividualFieldsRequest(TestConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Host))
        {
            return ConnectionErrors.HostRequired(nameof(request.Host));
        }

        if (string.IsNullOrWhiteSpace(request.DatabaseName))
        {
            return ConnectionErrors.DatabaseNameRequired(nameof(request.DatabaseName));
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return ConnectionErrors.UsernameRequired(nameof(request.Username));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ConnectionErrors.PasswordRequired(nameof(request.Password));
        }

        if (request.Port.HasValue && (request.Port.Value < 1 || request.Port.Value > 65535))
        {
            return ConnectionErrors.InvalidPort(request.Port.Value, nameof(request.Port));
        }

        return null;
    }

   



private static string ConnectionSummaryHelper(ConnectedDatabase connection)
{
    var summary = new
    {
        
        
        connection.DatabaseName,
        connection.Host,
        connection.PortNumber,
        connection.DatabaseProvider,
        connection.UseSSL,
        connection.IsHealthy,
        connection.LastSuccessfulConnection,
        connection.AdditionalParameters
        
    };

    return JsonSerializer.Serialize(summary, new JsonSerializerOptions
    {
        WriteIndented = true
    });
}

 }
