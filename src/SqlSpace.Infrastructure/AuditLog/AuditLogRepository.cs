using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Audit;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Identity;
using SqlSpace.Domain.Common.Errors;

namespace SqlSpace.Infrastructure.AuditLog;

public class AuditLogRepository(IApplicationDbContext DbContext,
                                ILogger<AuditLogRepository> logger,
                                UserManager<ApplicationUser> userManager) : IAuditLogRepository
{
    private readonly IApplicationDbContext _dbContext = DbContext;
    private readonly ILogger<AuditLogRepository> _logger = logger;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedAuditLogs>> GetConnectionAuditLogsAsync(Guid connectionId, int pageNumber, int pageSize, CancellationToken ct)
    {
        if (pageNumber <= 0)
        {
            return Result<PaginatedAuditLogs>.Failure(
                AuditLogErrors.InvalidPageNumber(nameof(pageNumber)));
        }

        if (pageSize <= 0)
        {
            return Result<PaginatedAuditLogs>.Failure(
                AuditLogErrors.InvalidPageSize(nameof(pageSize)));
        }

        try
        {
            var baseQuery = _dbContext.AccessAuditLogs
                .AsNoTracking()
                .Where(al => al.DatabaseConnectionId == connectionId);

            var totalCount = await baseQuery.CountAsync(ct);

            var itemsQuery =
                from log in baseQuery
                join actorUser in _userManager.Users.AsNoTracking()
                    on log.ActorUserId equals actorUser.Id into actorJoin
                from actor in actorJoin.DefaultIfEmpty()
                join targetUser in _userManager.Users.AsNoTracking()
                    on log.TargetUserId equals targetUser.Id into targetJoin
                from target in targetJoin.DefaultIfEmpty()
                orderby log.PerformedAt descending
                select new AuditLogDto
                {
                    AuditLogId = log.AuditLogId,
                    ActorUserEmail =  actor.Email ?? string.Empty  ,
                    TargetUserEmail = target.Email ?? string.Empty ,
                    ActorUserName = actor.UserName ?? string.Empty,
                    TargetUserName = target.UserName ?? string.Empty,
                    Action = log.Action,
                    Details = log.Details,
                    PerformedAt = log.PerformedAt
                };

            var items = await itemsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Result<PaginatedAuditLogs>.Success(new PaginatedAuditLogs
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch audit logs for connection {ConnectionId} (Page {PageNumber}, Size {PageSize}).",
                connectionId,
                pageNumber,
                pageSize);

            return Result<PaginatedAuditLogs>.Failure(
                AuditLogErrors.QueryFailed(nameof(connectionId)));
        }
    }

    public async Task<Result> LogAccessGrantedAsync(Guid connectionId, string actorUserId, string targetUserId, bool hasFullAccess, IReadOnlyList<string>? restrictedTables, CancellationToken cancellationToken)
    {
        var connectedDb = await _dbContext.ConnectedDatabases.FirstOrDefaultAsync(cd=>cd.ConnectionId == connectionId , cancellationToken);
        if(connectedDb is null)
        {
            return Result.Failure(AuditLogErrors.AccessGrantedFailed("Error Loading Connected database", nameof(connectionId)));
        }
        var actorUser = await  _userManager.FindByIdAsync(actorUserId);
        
        if( actorUser is null   )
        {
            return Result.Failure(AuditLogErrors.AccessGrantedFailed("Error Loading Users", nameof(actorUserId)));
        }
        
        var TargetUser = await _userManager.FindByIdAsync(targetUserId);

        if(TargetUser is null)
        {
            return Result.Failure(AuditLogErrors.AccessGrantedFailed("Error Loading Users", nameof(targetUserId)));
        }



        AccessAuditLog newLog = new AccessAuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TargetUserId =targetUserId,
            ActorUserId = actorUserId,
            Action = AccessAuditLogActions.Granted_Access,
            DatabaseConnectionId = connectionId,
            PerformedAt = DateTime.UtcNow,
            
        };

        var normalizedRestrictedTables = hasFullAccess ? Array.Empty<string>():
        (restrictedTables ?? Array.Empty<string>()).Where(t=>!string.IsNullOrWhiteSpace(t))
        .Select(t=>t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        newLog.Details = JsonSerializer.Serialize(new
        {
           hasFullAccess,
           restrictedTables = normalizedRestrictedTables 
        });

        try
        {
            await _dbContext.AccessAuditLogs.AddAsync(newLog , cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch(Exception ex)
        {
        _logger.LogError(
        ex,
        "Failed to persist access-granted audit log. ConnectionId: {ConnectionId}, ActorUserId: {ActorUserId}, TargetUserId: {TargetUserId}",
        connectionId,
        actorUserId,
        targetUserId);

        return Result.Failure(AuditLogErrors.AccessGrantedPersistFailed("failed to Persist access granted audit log", nameof(connectionId)));
        };

        
    }

    public async  Task<Result> LogAccessRevokedAsync(Guid connectionId, string actorUserId, string targetUserId, CancellationToken cancellationToken)
    {
        var connectedDb = await _dbContext.ConnectedDatabases.FirstOrDefaultAsync(cd=>cd.ConnectionId == connectionId, cancellationToken);
        if(connectedDb is null)
        {
            return Result.Failure(AuditLogErrors.AccessRevokedFailed("Error Loading Connected database", nameof(connectionId)));
        }
        var actorUser = await  _userManager.FindByIdAsync(actorUserId);
        
        if( actorUser is null   )
        {
            return Result.Failure(AuditLogErrors.AccessRevokedFailed("Error Loading Users", nameof(actorUserId)));
        }
        
        var TargetUser = await _userManager.FindByIdAsync(targetUserId);

        if(TargetUser is null)
        {
            return Result.Failure(AuditLogErrors.AccessRevokedFailed("Error Loading Users", nameof(targetUserId)));
        }
         AccessAuditLog RevokedLog = new AccessAuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TargetUserId =targetUserId,
            ActorUserId = actorUserId,
            Action = AccessAuditLogActions.Revoked_Access,
            DatabaseConnectionId = connectionId,
            PerformedAt = DateTime.UtcNow,
            
        };
          try
        {
            await _dbContext.AccessAuditLogs.AddAsync(RevokedLog , cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch(Exception ex)
        {
        _logger.LogError(
        ex,
        "Failed to persist access-Revocation audit log. ConnectionId: {ConnectionId}, ActorUserId: {ActorUserId}, TargetUserId: {TargetUserId}",
        connectionId,
        actorUserId,
        targetUserId);

        return Result.Failure(AuditLogErrors.AccessRevokedPersistFailed("failed to Persist access Revoked audit log", nameof(connectionId)));
        };




    }

    public async Task<Result> LogOwnershipTransferAsync(Guid connectionId, string previousAdminUserId, string newAdminUserId, CancellationToken cancellationToken)
    {
        var connectedDb = await _dbContext.ConnectedDatabases.FirstOrDefaultAsync(cd=>cd.ConnectionId == connectionId,cancellationToken);
        if(connectedDb is null)
        {
            return Result.Failure(AuditLogErrors.OwnershipTransferFailed("Error Loading Connected database", nameof(connectionId)));
        }
        var PreviousAdmin = await  _userManager.FindByIdAsync(previousAdminUserId);
        
        if( PreviousAdmin is null   )
        {
            return Result.Failure(AuditLogErrors.OwnershipTransferFailed("Error Loading Users", nameof(previousAdminUserId)));
        }
        
        var newAdmin = await _userManager.FindByIdAsync(newAdminUserId);

        if(newAdmin is null)
        {
            return Result.Failure(AuditLogErrors.OwnershipTransferFailed("Error Loading Users", nameof(newAdminUserId)));
        }
         AccessAuditLog RevokedLog = new AccessAuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TargetUserId =newAdminUserId,
            ActorUserId = previousAdminUserId,
            Action = AccessAuditLogActions.OwnershipTransferred,
            DatabaseConnectionId = connectionId,
            PerformedAt = DateTime.UtcNow,
            
        };
          try
        {
           
            await _dbContext.AccessAuditLogs.AddAsync(RevokedLog , cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch(Exception ex)
        {
        _logger.LogError(
        ex,
        "Failed to persist access-Transfer audit log. ConnectionId: {ConnectionId}, ActorUserId: {ActorUserId}, TargetUserId: {TargetUserId}",
        connectionId,
        previousAdminUserId,
        newAdminUserId);

        return Result.Failure(AuditLogErrors.AccessTransferPersistFailed("failed to Persist access Transfer audit log", nameof(connectionId)));
        };
    }

    public async Task<Result> LogRestrictionsUpdatedAsync(Guid connectionId, string actorUserId, string targetUserId, bool previousFullAccess, bool newFullAccess, IReadOnlyList<string>? previousRestrictions, IReadOnlyList<string>? newRestrictions, CancellationToken cancellationToken)
    {
        var connectedDb = await _dbContext.ConnectedDatabases.FirstOrDefaultAsync(
            cd => cd.ConnectionId == connectionId,
            cancellationToken);
        if (connectedDb is null)
        {
            return Result.Failure(AuditLogErrors.RestrictionsUpdatedFailed("Error Loading Connected database", nameof(connectionId)));
        }

        var actorUser = await _userManager.FindByIdAsync(actorUserId);
        if (actorUser is null)
        {
            return Result.Failure(AuditLogErrors.RestrictionsUpdatedFailed("Error Loading Users", nameof(actorUserId)));
        }

        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser is null)
        {
            return Result.Failure(AuditLogErrors.RestrictionsUpdatedFailed("Error Loading Users", nameof(targetUserId)));
        }

        var normalizedPreviousRestrictions = previousFullAccess
            ? Array.Empty<string>()
            : (previousRestrictions ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var normalizedNewRestrictions = newFullAccess
            ? Array.Empty<string>()
            : (newRestrictions ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var updatedLog = new AccessAuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TargetUserId = targetUserId,
            ActorUserId = actorUserId,
            Action = AccessAuditLogActions.PermissionsUpdated,
            DatabaseConnectionId = connectionId,
            PerformedAt = DateTime.UtcNow,
            Details = JsonSerializer.Serialize(new
            {
                previous = new
                {
                    hasFullAccess = previousFullAccess,
                    restrictedTables = normalizedPreviousRestrictions
                },
                current = new
                {
                    hasFullAccess = newFullAccess,
                    restrictedTables = normalizedNewRestrictions
                }
            })
        };

        try
        {
            await _dbContext.AccessAuditLogs.AddAsync(updatedLog, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist restrictions-updated audit log. ConnectionId: {ConnectionId}, ActorUserId: {ActorUserId}, TargetUserId: {TargetUserId}",
                connectionId,
                actorUserId,
                targetUserId);

            return Result.Failure(AuditLogErrors.RestrictionsUpdatedPersistFailed("failed to Persist restrictions updated audit log", nameof(connectionId)));
        }
    }
}
