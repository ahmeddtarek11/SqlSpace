using SqlSpace.Application.Abstractions.Audit;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Tests.AccessControl.Fakes;

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public bool FailGrantLog { get; set; }
    public bool FailRevokeLog { get; set; }
    public bool FailUpdateRestrictionsLog { get; set; }

    public int GrantCalls { get; private set; }
    public int RevokeCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Guid LastGrantConnectionId { get; private set; }
    public string LastGrantActorUserId { get; private set; } = string.Empty;
    public string LastGrantTargetUserId { get; private set; } = string.Empty;
    public bool LastGrantHasFullAccess { get; private set; }
    public IReadOnlyList<string>? LastGrantRestrictedTables { get; private set; }

    public Guid LastRevokeConnectionId { get; private set; }
    public string LastRevokeActorUserId { get; private set; } = string.Empty;
    public string LastRevokeTargetUserId { get; private set; } = string.Empty;

    public Guid LastUpdateConnectionId { get; private set; }
    public string LastUpdateActorUserId { get; private set; } = string.Empty;
    public string LastUpdateTargetUserId { get; private set; } = string.Empty;
    public bool LastUpdatePreviousFullAccess { get; private set; }
    public bool LastUpdateNewFullAccess { get; private set; }
    public IReadOnlyList<string>? LastUpdatePreviousRestrictions { get; private set; }
    public IReadOnlyList<string>? LastUpdateNewRestrictions { get; private set; }

    public Task<Result> LogAccessGrantedAsync(
        Guid connectionId,
        string actorUserId,
        string targetUserId,
        bool hasFullAccess,
        IReadOnlyList<string>? restrictedTables,
        CancellationToken cancellationToken)
    {
        GrantCalls++;
        LastGrantConnectionId = connectionId;
        LastGrantActorUserId = actorUserId;
        LastGrantTargetUserId = targetUserId;
        LastGrantHasFullAccess = hasFullAccess;
        LastGrantRestrictedTables = restrictedTables;

        if (FailGrantLog)
        {
            return Task.FromResult(Result.Failure(
                new Error("tests.audit.grant_failed", "Grant audit failure requested by test.")));
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> LogRestrictionsUpdatedAsync(
        Guid connectionId,
        string actorUserId,
        string targetUserId,
        bool previousFullAccess,
        bool newFullAccess,
        IReadOnlyList<string>? previousRestrictions,
        IReadOnlyList<string>? newRestrictions,
        CancellationToken cancellationToken)
    {
        UpdateCalls++;
        LastUpdateConnectionId = connectionId;
        LastUpdateActorUserId = actorUserId;
        LastUpdateTargetUserId = targetUserId;
        LastUpdatePreviousFullAccess = previousFullAccess;
        LastUpdateNewFullAccess = newFullAccess;
        LastUpdatePreviousRestrictions = previousRestrictions;
        LastUpdateNewRestrictions = newRestrictions;

        if (FailUpdateRestrictionsLog)
        {
            return Task.FromResult(Result.Failure(
                new Error("tests.audit.update_failed", "Restrictions update audit failure requested by test.")));
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> LogAccessRevokedAsync(
        Guid connectionId,
        string actorUserId,
        string targetUserId,
        CancellationToken cancellationToken)
    {
        RevokeCalls++;
        LastRevokeConnectionId = connectionId;
        LastRevokeActorUserId = actorUserId;
        LastRevokeTargetUserId = targetUserId;

        if (FailRevokeLog)
        {
            return Task.FromResult(Result.Failure(
                new Error("tests.audit.revoke_failed", "Revoke audit failure requested by test.")));
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> LogOwnershipTransferAsync(
        Guid connectionId,
        string previousAdminUserId,
        string newAdminUserId,
        CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());

    public Task<Result<PaginatedAuditLogs>> GetConnectionAuditLogsAsync(
        Guid connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var payload = new PaginatedAuditLogs
        {
            Items = new List<AuditLogDto>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Task.FromResult(Result<PaginatedAuditLogs>.Success(payload));
    }
}
