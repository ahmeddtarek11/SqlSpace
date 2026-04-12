using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Identity;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasMany(u => u.OwnedConnections)
            .WithOne()
            .HasForeignKey(c => c.DbAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AccessibleDatabases)
            .WithOne()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.QueriesHistory)
            .WithOne()
            .HasForeignKey(q => q.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AuditLogsAsActor)
            .WithOne()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AuditLogsAsTarget)
            .WithOne()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
