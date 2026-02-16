using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class ConnectedDatabaseConfiguration : IEntityTypeConfiguration<ConnectedDatabase>
{
    public void Configure(EntityTypeBuilder<ConnectedDatabase> builder)
    {
        builder.ToTable("ConnectedDatabases");

        builder.HasKey(c => c.ConnectionId);

        builder.Property(c => c.ConnectionId)
            .ValueGeneratedNever();

        builder.Property(c => c.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(c => c.DbAdminId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(c => c.ConnectionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Host)
            .HasMaxLength(255);

        builder.Property(c => c.PortNumber);

        builder.Property(c => c.DatabaseProvider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.Username)
            .HasMaxLength(256);

        builder.Property(c => c.EncryptedPassword);

        builder.Property(c => c.AdditionalParameters)
            .HasMaxLength(2000);

        builder.Property(c => c.EncryptedRawConnectionString);

        builder.Property(c => c.UseSSL)
            .IsRequired();

        builder.Property(c => c.UsesRawConnectionString)
            .IsRequired();

        builder.Property(c => c.LastSuccessfulConnection);

        builder.Property(c => c.IsHealthy)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.IsDeleted)
            .IsRequired();

        builder.Property(c => c.DeletedAt);

        builder.Property(c => c.DeletedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(c => new { c.DbAdminId, c.IsDeleted });
        builder.HasIndex(c => new { c.CreatedByUserId, c.IsDeleted });
        builder.HasIndex(c => new { c.ConnectionName, c.IsDeleted });
        builder.HasIndex(c => new { c.DbAdminId, c.ConnectionName, c.IsDeleted })
            .IsUnique();

        builder.HasMany(c => c.UserAccesses)
            .WithOne(a => a.DatabaseConnection)
            .HasForeignKey(a => a.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.SchemaSnapshots)
            .WithOne(s => s.DatabaseConnection)
            .HasForeignKey(s => s.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Queries)
            .WithOne(q => q.DatabaseConnection)
            .HasForeignKey(q => q.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.AuditLogs)
            .WithOne(a => a.DatabaseConnection)
            .HasForeignKey(a => a.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
