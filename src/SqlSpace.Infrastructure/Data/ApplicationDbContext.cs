using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Identity;

namespace SqlSpace.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConnectedDatabase> ConnectedDatabases => Set<ConnectedDatabase>();
    public DbSet<UserDatabaseAccess> UserDatabaseAccesses => Set<UserDatabaseAccess>();
    public DbSet<TableRestriction> TableRestrictions => Set<TableRestriction>();
    public DbSet<DatabaseSchemaSnapshot> DatabaseSchemaSnapshots => Set<DatabaseSchemaSnapshot>();
    public DbSet<QueryHistory> QueryHistories => Set<QueryHistory>();
    public DbSet<AccessAuditLog> AccessAuditLogs => Set<AccessAuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {

        // pre save logic 
            return base.SaveChangesAsync(cancellationToken);
    }

}
