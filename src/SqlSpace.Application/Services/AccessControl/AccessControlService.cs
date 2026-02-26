using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Audit;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.AccessControl;
// TO-DO 
// create a helper serialize and Deserialize helper functions 
public class AccessControlService(IApplicationDbContext context,
                                  IUserRepository userRepo,
                                  ILogger<AccessControlService> logger,
                                  IAuditLogRepository auditLog) : IAccessControlService

{
    private readonly IApplicationDbContext _context = context;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILogger<AccessControlService> _logger = logger;
    private readonly IAuditLogRepository _auditLog = auditLog;

    public async Task<Result<bool>> CanAccessTableAsync(Guid connectionId, string userId, string tableName, string? schemaName, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return AccessControlErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return AccessControlErrors.InvalidTargetUserId(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            return AccessControlErrors.InvalidTableName(nameof(tableName));
        }

        var normalizedTableName = tableName.Trim();
        var normalizedSchemaName = string.IsNullOrWhiteSpace(schemaName) ? null : schemaName.Trim();

        try
        {
            var database = await _context.ConnectedDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    db => db.ConnectionId == connectionId && !db.IsDeleted,
                    cancellationToken);

            if (database is null)
            {
                return AccessControlErrors.ConnectionNotFound(nameof(connectionId));
            }

            if (string.Equals(database.DbAdminId, userId, StringComparison.Ordinal))
            {
                return true;
            }

            var userAccess = await _context.UserDatabaseAccesses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.DatabaseConnectionId == connectionId
                        && a.UserId == userId
                        && !a.IsDeleted,
                    cancellationToken);

            if (userAccess is null)
            {

                return false;
            }

            if (userAccess.HasFullAccess)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(userAccess.RestrictedTablesJson))
            {
                _logger.LogWarning("User restriction Tables is null - possible data corruption try again and make sure that user is not an admin");
                return Result<bool>.Failure(AccessControlErrors.Unexpected(nameof(userAccess.RestrictedTablesJson)) ,"User restriction Tables is null - possible data corruption try again and make sure that user is not an admin");
            }

            bool isRestricted;
            try
            {
                isRestricted = userAccess.RestrictedTablesJson.IsTableRestricted(
                    database.DatabaseProvider,
                    normalizedTableName,
                    normalizedSchemaName);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid RestrictedTablesJson format for access record {AccessId}. Denying access by default.",
                    userAccess.Id);
                return false;
            }

            return !isRestricted;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to evaluate table access. ConnectionId: {ConnectionId}, UserId: {UserId}, TableName: {TableName}, SchemaName: {SchemaName}",
                connectionId,
                userId,
                normalizedTableName,
                normalizedSchemaName);

            return AccessControlErrors.QueryFailed("Failed to evaluate table access.", nameof(connectionId));
        }
    }

    
//---------------------------------------------------------------------------------------------------------------------------//
    public async Task<Result<ICollection<string>>> GetAccessibleTableNamesAsync(Guid connectionId, string userId, CancellationToken cancellationToken)
    {   

        // .....................................................refactor to-do........................................................................//
        ///That’s 4 responsibilities in one method.
            // It violates SRP 
            //  should extract:

            // ParseSchemaTables()

            // ParseRestrictedTables()

            // FilterAllowedTables()
            

        if(connectionId == Guid.Empty)
        {
            return Result<ICollection<string>>.Failure(AccessControlErrors.InvalidConnectionId(nameof(connectionId)));
        }
        if(userId is null)
        {
             return AccessControlErrors.InvalidTargetUserId(nameof(userId));
        }


        
        try{


            var connection = await  _context.ConnectedDatabases.AsNoTracking()
            .FirstOrDefaultAsync(c=>c.ConnectionId == connectionId && !c.IsDeleted , cancellationToken);
            if(connection is null)
            {
                return AccessControlErrors.ConnectionNotFound(nameof(connectionId));
            }


            if(string.Equals(connection.DbAdminId , userId , StringComparison.Ordinal))
            {
                var allTables = await LoadAllTablesFromSchemaAsync(connectionId ,connection.DatabaseProvider ,cancellationToken);
                return allTables;
            }

            var userAccess = await  _context.UserDatabaseAccesses.AsNoTracking()
            .FirstOrDefaultAsync(ua=>
            ua.UserId == userId
            && ua.DatabaseConnectionId == connectionId 
            &&!ua.IsDeleted,
            cancellationToken);
        
        
        if(userAccess is null )
        {
           return new List<string>();
        }
        
        var schema = await  _context.DatabaseSchemaSnapshots.AsNoTracking().FirstOrDefaultAsync(
            sc=>sc.DatabaseConnectionId == connectionId
            && sc.IsLatest 
            ,cancellationToken 
            ); 
        if(schema is null || string.IsNullOrWhiteSpace(schema.SchemaText))
        {
            return AccessControlErrors.SchemaSnapshotNotFound("Cannot find the Latest Schema for this for Provided Database Connection");
        }



        var schemaObject = JsonSerializer.Deserialize<SchemaSnapShotModel>(schema.SchemaText);
        if (schemaObject is null || schemaObject.Tables is null){
            return AccessControlErrors.SchemaSnapshotNotFound("invalid schema format");
        }
        
        //parse schema tables to a list with schema tables prefix with then a point then table name => schemaName.TableName 
        var schemaTables  = schemaObject.Tables.Select(t=> connection.DatabaseProvider.BuildTableKey(t.Name, t.Schema))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

            if (userAccess.HasFullAccess){
                return schemaTables;
            }

            // handling data corruption 
            if (string.IsNullOrWhiteSpace(userAccess.RestrictedTablesJson))
            {
                _logger.LogWarning(
                    "User {UserId} has HasFullAccess=false but no restrictions for connection:{connectionId}. Denying access .......",
                    userId,connectionId);
                return new List<string>();  // Fail-safe: deny access
            }


            var restrictedTables = JsonSerializer.Deserialize<List<TableRestrictionDto>>(userAccess.RestrictedTablesJson);

            if (restrictedTables is null){

                _logger.LogWarning(
                        "Failed to deserialize restrictions for user {UserId}.",
                        userId);
                    return new List<string>();
            }


            // hashSet lookUps in O(1) instead of looping and using extension method in O(N)
            HashSet<string> restrictedTablesSet = restrictedTables.
            Select(rt=>connection.DatabaseProvider.BuildTableKey(rt.Table, rt.Schema))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allowedTables = schemaTables
            .Where(t => !restrictedTablesSet.Contains(t))
            .ToList();

            return allowedTables;





        }

        catch(JsonException jex)
        {
             _logger.LogWarning(jex, "Invalid JSON format in schema or restrictions for connection:{ConnectionId}" , connectionId);
            return AccessControlErrors.Unexpected("Invalid data format");
        }
        catch (Exception ex)
        {
            _logger.LogError(
            ex,
            "Failed to get accessible tables. ConnectionId: {ConnectionId}, UserId: {UserId}",
            connectionId,
            userId);

            return AccessControlErrors.QueryFailed(
                "Failed to get accessible tables",
                nameof(connectionId));
        }

        
       
    }


    //-------------------------------------------------------------------------------------------------------------------------------------//

    public async Task<Result<bool>> HasAccessToConnectionAsync(Guid connectionId, string userId, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return AccessControlErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return AccessControlErrors.InvalidTargetUserId(nameof(userId));
        }


        var connection = await  _context.ConnectedDatabases.AsNoTracking()
        .Include(cd=> cd.UserAccesses.Where(ua=>ua.UserId == userId && !ua.IsDeleted))
        .FirstOrDefaultAsync(cd=>cd.ConnectionId == connectionId 
        && !cd.IsDeleted ,cancellationToken);

        if(connection is null)
        {
            return AccessControlErrors.ConnectionNotFound(nameof(connectionId)); 
        }
        if (string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal) 
            || connection.UserAccesses.Any())
        {
            return true;
        }

        return false;
        
        
    }

    //-------------------------------------------------------------------------------------------------------------------------------------//


    public async Task<Result<ICollection<UserAccessSummary>>> ListConnectionUsersAsync(Guid connectionId, string adminUserId, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return AccessControlErrors.InvalidConnectionId(nameof(connectionId)); 
        }

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return AccessControlErrors.InvalidAdminUserId(nameof(adminUserId));
        }

    try
    {

        var connection = await  _context.ConnectedDatabases.AsNoTracking()
        .FirstOrDefaultAsync(db=>db.ConnectionId == connectionId && !db.IsDeleted ,cancellationToken);

        if(connection is null)
        {
            return AccessControlErrors.ConnectionNotFound(nameof(connectionId));
        }

        if(!string.Equals(connection.DbAdminId , adminUserId , StringComparison.Ordinal))
        {
            return AccessControlErrors.AdminNotOwner(nameof(adminUserId));
        }

        var accesses = await _context.UserDatabaseAccesses.AsNoTracking()
        .Where(A=>A.DatabaseConnectionId == connectionId 
        && !A.IsDeleted && A.RevokedAt == null).OrderByDescending(a=>a.GrantedAt)
        .ToListAsync();

        if(accesses.Count == 0)
        {
            return new List<UserAccessSummary>();

        }

        var IdsToLoad = accesses.Select(a=>a.UserId)
        .Concat(accesses.Select(a=>a.GrantedByUserId))
        .Where(id=>!string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.Ordinal).ToArray();

        var users = await _userRepo.GetByIdsAsync(IdsToLoad, cancellationToken);
        var usersById = users.ToDictionary(u=>u.Id ,StringComparer.Ordinal);
        var UsersSummaries = new List<UserAccessSummary>(accesses.Count);

        foreach(var access in accesses)
        {
            usersById.TryGetValue(access.UserId , out var targetUser);
            usersById.TryGetValue(access.GrantedByUserId , out var grantedByUser);

            IReadOnlyList<string> restrictedTables = Array.Empty<string>();

             if (!access.HasFullAccess && !string.IsNullOrWhiteSpace(access.RestrictedTablesJson))
                {
                    try
                    {
                         restrictedTables = access.RestrictedTablesJson
                         .GetRestrictedTables(connection.DatabaseProvider)
                         .Select(rt=>connection.DatabaseProvider.BuildTableKey(rt.Table, rt.Schema))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
                    }

                    catch(JsonException ex)
                    {
                        _logger.LogWarning(
                        ex,
                        "Invalid RestrictedTablesJson for access record {AccessId}. Returning empty restrictions.",
                        access.Id);
                    }
                }

            UsersSummaries.Add(new UserAccessSummary
            {
                AccessId = access.Id,
                UserId = access.UserId,
                UserEmail = targetUser?.Email ?? string.Empty,
                UserName = targetUser?.UserName ?? string.Empty,
                HasFullAccess = access.HasFullAccess,
                RestrictedTables = restrictedTables,
                GrantedAt = access.GrantedAt,
                GrantedByUserEmail = grantedByUser?.Email ?? string.Empty
            });

        }
        return UsersSummaries;
    }

        catch(Exception ex )
        {
            _logger.LogError(
            ex,
            "Failed to list connection users. ConnectionId: {ConnectionId}, AdminUserId: {AdminUserId}",
            connectionId,
            adminUserId);

        return AccessControlErrors.QueryFailed("Failed to list connection users.", nameof(connectionId));
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------//


    public async Task<Result<bool>> RevokeAccessAsync(Guid connectionId, string adminUserId, string targetUserId, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return AccessControlErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return AccessControlErrors.InvalidAdminUserId(nameof(adminUserId));
        }

        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return AccessControlErrors.InvalidTargetUserId(nameof(targetUserId));
        }

        try
        {
            var connection = await _context.ConnectedDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    db => db.ConnectionId == connectionId && !db.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                return AccessControlErrors.ConnectionNotFound(nameof(connectionId));
            }

            if (!string.Equals(connection.DbAdminId, adminUserId, StringComparison.Ordinal))
            {
                return AccessControlErrors.AdminNotOwner(nameof(adminUserId));
            }

            var userAccess = await _context.UserDatabaseAccesses
                .FirstOrDefaultAsync(
                    ua => ua.DatabaseConnectionId == connectionId
                        && ua.UserId == targetUserId
                        && !ua.IsDeleted
                        && ua.RevokedAt == null,
                    cancellationToken);

            if (userAccess is null)
            {
                 _logger.LogWarning("User with id:{targetUserId} , doesn't have access to Database-connection with Id:{connectionId} to be revoked , Revocation failed" , targetUserId ,connectionId);
                return false;
               
            }

            userAccess.IsDeleted = true;
            userAccess.RevokedAt = DateTime.UtcNow;
            userAccess.RevokedByUserId = adminUserId;

            await _context.SaveChangesAsync(cancellationToken);

            var auditResult = await _auditLog.LogAccessRevokedAsync(
                connectionId,
                adminUserId,
                targetUserId,
                cancellationToken);

            if (auditResult.IsFailure)
            {
                _logger.LogWarning(
                    "Access revoked but audit logging failed for ConnectionId {ConnectionId}, AdminUserId {AdminUserId}, TargetUserId {TargetUserId}.",
                    connectionId,
                    adminUserId,
                    targetUserId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to revoke access. ConnectionId: {ConnectionId}, AdminUserId: {AdminUserId}, TargetUserId: {TargetUserId}",
                connectionId,
                adminUserId,
                targetUserId);

            return AccessControlErrors.RevokeAccessFailed("Failed to revoke access.", nameof(connectionId));
        }
    }



     //------------------------------------------------------------------------------------------------------------------------------------//

    public async  Task<Result<UserAccessSummary>> GrantAccessAsync(Guid connectionId, string adminUserId, 
    string targetUserEmail, bool hasFullAccess, IReadOnlyList<TableRestrictionInput>? restrictedTables, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return AccessControlErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return AccessControlErrors.InvalidAdminUserId(nameof(adminUserId));
        }

        if (string.IsNullOrWhiteSpace(targetUserEmail))
        {
            return AccessControlErrors.InvalidTargetUserEmail(nameof(targetUserEmail));
        }

        var connection = await _context.ConnectedDatabases.AsNoTracking().FirstOrDefaultAsync(db=>
        db.ConnectionId == connectionId 
        && !db.IsDeleted , cancellationToken
        );

        if(connection is null)
        {
            return AccessControlErrors.ConnectionNotFound($"cannot find a connection with the provided Id:{connectionId}");
        }

        if(!string.Equals(adminUserId , connection.DbAdminId , StringComparison.Ordinal))
        {
            return AccessControlErrors.AdminNotOwner();
        }

        var user = await  _userRepo.GetByEmailAsync(targetUserEmail, cancellationToken);
        var admin =  await _userRepo.GetByIdAsync(adminUserId ,cancellationToken);
        if(user is null)
        {
            _logger.LogError("Cannot find a user with the Provided Email:{targetUserEmail} , Access granting Failed .. " , targetUserEmail);
            return AccessControlErrors.TargetUserNotFound();
        }
        if(admin is null)
        {
            _logger.LogError("Cannot find a user with the UserId:{AdminId} , Access granting Failed .. " , adminUserId);
            return AccessControlErrors.TargetUserNotFound();
        }

        // check if user already has access 
            var existingAccess = await _context.UserDatabaseAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.DatabaseConnectionId == connectionId 
                    && a.UserId == user.Id 
                    && !a.IsDeleted,
                cancellationToken);

            if (existingAccess != null)
            {
                _logger.LogWarning(
                    "User {UserId} already has access to connection {ConnectionId}",
                    user.Id,
                    connectionId);
                return AccessControlErrors.GrantAccessFailed(
                    $"User {targetUserEmail} already has access to this connection");
            }


        var newAccess = new UserDatabaseAccess{
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    DatabaseConnectionId = connectionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedByUserId = adminUserId,
                    RevokedAt = null ,
                    RevokedByUserId = null 

                } ;

        

        if (hasFullAccess)
        {
              newAccess.RestrictedTablesJson = null;
               newAccess.HasFullAccess =true;

        }

        else{
        // Validate restrictions exist
        if (restrictedTables is null || restrictedTables.Count == 0)
        {
            return AccessControlErrors.GrantAccessFailed(
                "Must provide at least one restricted table when not granting full access");
        }

        // Apply default schemas based on database provider
        var normalizedRestrictions = restrictedTables
            .Select(rt => new TableRestrictionDto
            {
                Schema = connection.DatabaseProvider.NormalizeSchema(rt.Schema),
                Table = rt.Table?.Trim() ?? string.Empty
            })
            .Where(rt => !string.IsNullOrWhiteSpace(rt.Table))
            .ToList();

       // Validate each restriction
        foreach (var restriction in normalizedRestrictions)
        {
           
            if (string.IsNullOrWhiteSpace(restriction.Table))
            {
                return AccessControlErrors.GrantAccessFailed("Table name cannot be empty");
            }
        }

        // Check for duplicates
        var duplicates = normalizedRestrictions
            .GroupBy(r => connection.DatabaseProvider.BuildTableKey(r.Table, r.Schema), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            return AccessControlErrors.GrantAccessFailed(
                $"Duplicate restrictions found: {string.Join(", ", duplicates)}");
        }

    try
    {
        var tablesRestricted = JsonSerializer.Serialize(normalizedRestrictions);
        newAccess.RestrictedTablesJson = tablesRestricted;
        newAccess.HasFullAccess = false;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Error serializing restricted tables");
        return AccessControlErrors.GrantAccessFailed("Failed to serialize restrictions");
    }
}
        
        
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var res = await _context.UserDatabaseAccesses.AddAsync(newAccess, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var log = await _auditLog.LogAccessGrantedAsync(
                connectionId,
                adminUserId,
                user.Id,
                hasFullAccess,
                restrictedTables?.Select(t => connection.DatabaseProvider.BuildTableKey(t.Table, t.Schema)).ToList(),
                cancellationToken);

            if (log.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AccessControlErrors.GrantAccessFailed("Failed to write access grant audit log.", nameof(connectionId));
            }

            await transaction.CommitAsync(cancellationToken);

            var rt = restrictedTables?
                .Select(t => connection.DatabaseProvider.BuildTableKey(t.Table, t.Schema))
                .ToList() ?? new List<string>();

            return new UserAccessSummary
            {
                AccessId = newAccess.Id,
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.UserName,
                HasFullAccess = newAccess.HasFullAccess,
                GrantedAt = newAccess.GrantedAt,
                GrantedByUserEmail = admin.Email,
                RestrictedTables = rt
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Failed to grant access. ConnectionId: {ConnectionId}, AdminUserId: {AdminUserId}, TargetUserEmail: {TargetUserEmail}",
                connectionId,
                adminUserId,
                targetUserEmail);
            return AccessControlErrors.GrantAccessFailed("Failed to grant access.", nameof(connectionId));
        }


    }


    //-------------------------------------------------------------------------------------------------------------------------------------//

    public async Task<Result> UpdateAccessRestrictionsAsync(
        Guid connectionId,
        string adminUserId,
        string targetUserId,
        bool hasFullAccess,
        IReadOnlyList<TableRestrictionInput>? restrictedTables,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return AccessControlErrors.InvalidConnectionId(nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return AccessControlErrors.InvalidAdminUserId(nameof(adminUserId));
        }

        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return AccessControlErrors.InvalidTargetUserId(nameof(targetUserId));
        }

        try
        {
            var connection = await _context.ConnectedDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    db => db.ConnectionId == connectionId && !db.IsDeleted,
                    cancellationToken);

            if (connection is null)
            {
                return AccessControlErrors.ConnectionNotFound(nameof(connectionId));
            }

            if (!string.Equals(connection.DbAdminId, adminUserId, StringComparison.Ordinal))
            {
                return AccessControlErrors.AdminNotOwner(nameof(adminUserId));
            }

            var userAccess = await _context.UserDatabaseAccesses
                .FirstOrDefaultAsync(
                    ua => ua.DatabaseConnectionId == connectionId
                        && ua.UserId == targetUserId
                        && !ua.IsDeleted
                        && ua.RevokedAt == null,
                    cancellationToken);

            if (userAccess is null)
            {
                return AccessControlErrors.AccessNotFound(nameof(targetUserId));
            }

            var previousFullAccess = userAccess.HasFullAccess;
            IReadOnlyList<string> previousRestrictions;

            try
            {
                previousRestrictions = userAccess.RestrictedTablesJson
                    .GetRestrictedTables(connection.DatabaseProvider)
                    .Select(rt => connection.DatabaseProvider.BuildTableKey(rt.Table, rt.Schema))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid previous RestrictedTablesJson for access record {AccessId}.",
                    userAccess.Id);
                previousRestrictions = Array.Empty<string>();
            }

            string? newRestrictionsJson = null;
            IReadOnlyList<string> newRestrictions = Array.Empty<string>();

            if (!hasFullAccess)
            {
                if (restrictedTables is null)
                {
                    return AccessControlErrors.UpdateRestrictionsFailed(
                        "Restricted tables are required when full access is disabled.",
                        nameof(restrictedTables));
                }

                var normalizedRestrictions = restrictedTables
                    .Where(rt => rt is not null)
                    .Select(rt => new TableRestrictionDto
                    {
                        Schema = connection.DatabaseProvider.NormalizeSchema(rt.Schema),
                        Table = rt.Table?.Trim() ?? string.Empty
                    })
                    .Where(rt => !string.IsNullOrWhiteSpace(rt.Table))
                    .GroupBy(rt => connection.DatabaseProvider.BuildTableKey(rt.Table, rt.Schema), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();

                if (normalizedRestrictions.Count == 0)
                {
                    return AccessControlErrors.UpdateRestrictionsFailed(
                        "At least one valid restricted table is required when full access is disabled.",
                        nameof(restrictedTables));
                }

                newRestrictionsJson = JsonSerializer.Serialize(normalizedRestrictions);
                newRestrictions = normalizedRestrictions
                    .Select(rt => connection.DatabaseProvider.BuildTableKey(rt.Table, rt.Schema))
                    .ToList();
            }

            userAccess.HasFullAccess = hasFullAccess;
            userAccess.RestrictedTablesJson = newRestrictionsJson;

            await _context.SaveChangesAsync(cancellationToken);

            var auditResult = await _auditLog.LogRestrictionsUpdatedAsync(
                connectionId,
                adminUserId,
                targetUserId,
                previousFullAccess,
                hasFullAccess,
                previousRestrictions,
                newRestrictions,
                cancellationToken);

            if (auditResult.IsFailure)
            {
                _logger.LogWarning(
                    "Access restrictions updated but audit logging failed for ConnectionId {ConnectionId}, AdminUserId {AdminUserId}, TargetUserId {TargetUserId}.",
                    connectionId,
                    adminUserId,
                    targetUserId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update access restrictions. ConnectionId: {ConnectionId}, AdminUserId: {AdminUserId}, TargetUserId: {TargetUserId}",
                connectionId,
                adminUserId,
                targetUserId);

            return AccessControlErrors.UpdateRestrictionsFailed(
                "Failed to update access restrictions.",
                nameof(connectionId));
        }
    }



    private  async Task<List<string>> LoadAllTablesFromSchemaAsync(Guid connectionId,DbProviders provider ,CancellationToken cancellationToken)
    {
        var schema = await _context.DatabaseSchemaSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.DatabaseConnectionId == connectionId && s.IsLatest,
                cancellationToken);

        if (schema is null || string.IsNullOrWhiteSpace(schema.SchemaText))
        {
            return new List<string>();
        }

        var schemaObject = JsonSerializer.Deserialize<SchemaSnapShotModel>(schema.SchemaText);
        
        return schemaObject?.Tables
            ?.Select(t => provider.BuildTableKey(t.Name, t.Schema))
            .ToList() ?? new List<string>();
    }


}










































































