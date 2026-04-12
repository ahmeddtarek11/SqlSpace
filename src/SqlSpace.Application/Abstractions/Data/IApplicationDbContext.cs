using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Data;

/// <summary>
/// Thin abstraction over the EF Core DbContext used by application services.
/// </summary>
/// <remarks>
/// Where:
/// - Interface consumed by Application layer.
/// - Implemented in Infrastructure by ApplicationDbContext.
/// </remarks>
public interface IApplicationDbContext
{
    DbSet<ConnectedDatabase> ConnectedDatabases { get; }
    DbSet<UserDatabaseAccess> UserDatabaseAccesses { get; }
    
    DbSet<DatabaseSchemaSnapshot> DatabaseSchemaSnapshots { get; }
    DbSet<QueryHistory> QueryHistories { get; }
    DbSet<SavedQuery> SavedQueries { get; }
    DbSet<AccessAuditLog> AccessAuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SavedChart> SavedCharts { get; }

    //DbSet<TableRestriction> TableRestrictions { get; }
    // deprecated

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
