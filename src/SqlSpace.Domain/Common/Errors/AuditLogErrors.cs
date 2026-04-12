using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Domain.Common.Errors;

public static class AuditLogErrors
{
    public const string InvalidPageNumberCode = "audit.invalid_page_number";
    public const string InvalidPageSizeCode = "audit.invalid_page_size";
    public const string QueryFailedCode = "audit.query_failed";
    public const string AccessGrantedFailedCode = "audit.access_granted_failed";
    public const string AccessGrantedPersistFailedCode = "audit.access_granted_persist_failed";
    public const string AccessRevokedFailedCode = "audit.access_revoked_failed";
    public const string AccessRevokedPersistFailedCode = "audit.access_revoked_persist_failed";
    public const string OwnershipTransferFailedCode = "audit.ownership_transfer_failed";
    public const string AccessTransferPersistFailedCode = "audit.access_transfer_persist_failed";
    public const string RestrictionsUpdatedFailedCode = "audit.restrictions_updated_failed";
    public const string RestrictionsUpdatedPersistFailedCode = "audit.restrictions_updated_persist_failed";

    public static Error InvalidPageNumber(string? target = null) =>
        new(InvalidPageNumberCode, "Page number must be greater than zero.", target);

    public static Error InvalidPageSize(string? target = null) =>
        new(InvalidPageSizeCode, "Page size must be greater than zero.", target);

    public static Error QueryFailed(string? target = null) =>
        new(QueryFailedCode, "Failed to retrieve connection audit logs.", target);

    public static Error AccessGrantedFailed(string message, string? target = null) =>
        new(AccessGrantedFailedCode, message, target);

    public static Error AccessGrantedPersistFailed(string message, string? target = null) =>
        new(AccessGrantedPersistFailedCode, message, target);

    public static Error AccessRevokedFailed(string message, string? target = null) =>
        new(AccessRevokedFailedCode, message, target);

    public static Error AccessRevokedPersistFailed(string message, string? target = null) =>
        new(AccessRevokedPersistFailedCode, message, target);

    public static Error OwnershipTransferFailed(string message, string? target = null) =>
        new(OwnershipTransferFailedCode, message, target);

    public static Error AccessTransferPersistFailed(string message, string? target = null) =>
        new(AccessTransferPersistFailedCode, message, target);

    public static Error RestrictionsUpdatedFailed(string message, string? target = null) =>
        new(RestrictionsUpdatedFailedCode, message, target);

    public static Error RestrictionsUpdatedPersistFailed(string message, string? target = null) =>
        new(RestrictionsUpdatedPersistFailedCode, message, target);
}
