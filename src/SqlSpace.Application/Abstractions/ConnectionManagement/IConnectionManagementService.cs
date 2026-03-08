using SqlSpace.Application.Abstractions.ConnectionManagement.Dtos;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Connections;

/// <summary>
/// Manages database connection lifecycle including creation, testing, updates, and deletion.
/// </summary>
/// <remarks>
/// Usage:
/// - Primary service for all connection-related operations exposed through API.
/// - Handles both simple (individual fields) and advanced (raw connection string) input modes.
///
/// When:
/// - User creates new database connection from UI.
/// - User tests connection before saving.
/// - User updates connection password or settings.
/// - User deletes a connection.
///
/// Why:
/// - Centralizes connection management business logic and validation.
/// - Coordinates encryption, connection testing, and schema extraction.
/// - Enforces ownership rules (only admin can modify/delete).
///
/// Where:
/// - Interface consumed by API controllers.
/// - Implemented in Application layer as a connection-management use-case service.
///
/// How:
/// - Validate and encrypt connection credentials.
/// - Test connectivity before persisting.
/// - Trigger background schema extraction after creation.
/// - Enforce admin-only modifications.
/// </remarks>
public interface IConnectionManagementService
{
    /// <summary>
    /// Creates a new database connection with validation and initial schema extraction.
    /// </summary>
    /// <param name="userId">User identifier who will own the connection (becomes admin).</param>
    /// <param name="request">Connection creation request with credentials and settings.</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns>Created connection entity with assigned identifier.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate request (required fields, port range, etc.).
    /// 2. Check for duplicate connection name for this user.
    /// 3. If simple mode, validate individual components.
    /// 4. If advanced mode, parse and validate raw connection string.
    /// 5. Test connection before saving (verify credentials work).
    /// 6. Encrypt password or full connection string.
    /// 7. Create ConnectedDatabase entity.
    /// 8. Set user as admin (AdminUserId).
    /// 9. Persist to database.
    /// 10. Enqueue background job for schema extraction.
    /// 11. Return created connection (without decrypted credentials).
    /// </remarks>
    Task<Result<ConnectionCreationResponse>> CreateConnectionAsync(
        string userId,
        CreateConnectionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tests database connectivity without persisting connection.
    /// </summary>
    /// <param name="request">Connection test request with credentials.</param>
    /// <param name="cancellationToken">Cancellation token with shorter timeout (10 seconds).</param>
    /// <returns>Test result with success flag, server info, and extracted connection details.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate request parameters.
    /// 2. Build temporary ConnectedDatabase entity (not persisted).
    /// 3. Attempt connection using IDatabaseExecutor.
    /// 4. Capture server version and connection latency.
    /// 5. If advanced mode, parse connection string to extract components.
    /// 6. Return test result with server metadata.
    /// 7. Return error details if connection fails.
    /// </remarks>
    Task<Result<ConnectionTestResult>> TestNewConnectionAsync(
        TestConnectionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the password for an existing connection.
    /// </summary>
    /// <param name="connectionId">Connection identifier to update.</param>
    /// <param name="userId">User identifier making the change (must be admin).</param>
    /// <param name="newPassword">New plain text password to encrypt and store.</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns><c>true</c> if password updated successfully; <c>false</c> if unauthorized or not found.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection from database.
    /// 2. Verify requesting user is connection admin.
    /// 3. Test connection with new password (verify it works).
    /// 4. Encrypt new password.
    /// 5. Update EncryptedPassword field.
    /// 6. Update LastSuccessfulConnection timestamp.
    /// 7. Persist changes.
    /// 8. Return success indicator.
    /// </remarks>
    Task<Result<bool>> UpdatePasswordAsync(
        Guid connectionId,
        string userId,
        string newPassword,
        CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a database connection (sets IsDeleted flag).
    /// </summary>
    /// <param name="connectionId">Connection identifier to delete.</param>
    /// <param name="userId">User identifier performing deletion (must be admin).</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns><c>true</c> if deleted successfully; <c>false</c> if unauthorized or not found.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection from database.
    /// 2. Verify requesting user is connection admin.
    /// 3. Set IsDeleted = true.
    /// 4. Set DeletedAt timestamp.
    /// 5. Set DeletedByUserId.
    /// 6. Soft-delete all associated UserDatabaseAccess records.
    /// 7. Persist changes atomically.
    /// 8. Return success indicator.
    /// </remarks>
    Task<Result<bool>> DeleteConnectionAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single connection by identifier with decrypted view for admin.
    /// </summary>
    /// <param name="connectionId">Connection identifier to retrieve.</param>
    /// <param name="userId">User identifier requesting connection (must have access).</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Connection details DTO without sensitive credentials, or null if not found/unauthorized.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection from database.
    /// 2. Check if requesting user is admin or has active access.
    /// 3. Map to DTO (exclude encrypted credentials from response).
    /// 4. Include connection health status and last test timestamp.
    /// 5. Return DTO or null if unauthorized.
    /// </remarks>
    Task<Result<ConnectionDto?>> GetConnectionByIdAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists all database connections accessible by a user (owned + granted access).
    /// </summary>
    /// <param name="userId">User identifier to list connections for.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>List of connection summary DTOs.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query connections where user is admin.
    /// 2. Union with connections where user has active access grant.
    /// 3. Exclude soft-deleted connections.
    /// 4. Map to summary DTOs (name, type, health, role).
    /// 5. Return list ordered by creation date descending.
    /// </remarks>
    Task<Result<IReadOnlyList<ConnectionSummaryDto>>> GetUserConnectionsAsync(
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Transfers connection ownership to another user.
    /// </summary>
    /// <param name="connectionId">Connection identifier to transfer.</param>
    /// <param name="currentAdminUserId">Current owner user identifier.</param>
    /// <param name="newAdminEmail">Email of user to become new owner.</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns><c>true</c> if ownership transferred; <c>false</c> if unauthorized or target user not found.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection from database.
    /// 2. Verify requesting user is current admin.
    /// 3. Resolve target user by email.
    /// 4. Update AdminUserId to new user.
    /// 5. Grant old admin regular user access if desired.
    /// 6. Log ownership transfer to audit trail.
    /// 7. Persist changes.
    /// 8. Return success indicator.
    /// </remarks>
    Task<Result<bool>> TransferOwnershipAsync(
        Guid connectionId,
        string currentAdminUserId,
        string newAdminEmail,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tests health of an existing connection and updates status.
    /// </summary>
    /// <param name="connectionId">Connection identifier to test.</param>
    /// <param name="cancellationToken">Cancellation token for test operation.</param>
    /// <returns>Test result with updated health status.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection from database.
    /// 2. Decrypt credentials.
    /// 3. Attempt connection test.
    /// 4. Update IsHealthy flag based on result.
    /// 5. Update LastSuccessfulConnection if successful.
    /// 6. Update LastConnectionError if failed.
    /// 7. Persist changes.
    /// 8. Return test result.
    /// </remarks>
    Task<Result<ConnectionTestResult>> TestExistingConnectionHealthAsync(
        Guid connectionId,
        CancellationToken cancellationToken);
}
