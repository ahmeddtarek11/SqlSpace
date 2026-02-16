using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class AccessAuditLogConfiguration : IEntityTypeConfiguration<AccessAuditLog>
{
    public void Configure(EntityTypeBuilder<AccessAuditLog> builder)
    {
        builder.ToTable("AccessAuditLogs");

        builder.HasKey(al => al.AuditLogId);

        builder.Property(al => al.AuditLogId)
            .ValueGeneratedNever();

        builder.Property(al => al.DatabaseConnectionId)
            .IsRequired();

        builder.Property(al => al.ActorUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(al => al.TargetUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(al => al.Details)
            .HasMaxLength(2000);

        builder.Property(al => al.PerformedAt)
            .IsRequired();

        builder.HasIndex(al => new { al.DatabaseConnectionId, al.PerformedAt });
        builder.HasIndex(al => new { al.ActorUserId, al.PerformedAt });
        builder.HasIndex(al => new { al.TargetUserId, al.PerformedAt });

        builder.HasOne(al => al.DatabaseConnection)
            .WithMany(db => db.AuditLogs)
            .HasForeignKey(al => al.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
