using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class DatabaseSchemaSnapshotConfiguration : IEntityTypeConfiguration<DatabaseSchemaSnapshot>
{
    public void Configure(EntityTypeBuilder<DatabaseSchemaSnapshot> builder)
    {
        builder.ToTable("DatabaseSchemaSnapshots");

        builder.HasKey(s => s.SnapshotId);

        builder.Property(s => s.SnapshotId)
            .ValueGeneratedNever();

        builder.Property(s => s.DatabaseConnectionId)
            .IsRequired();

        builder.Property(s => s.SchemaText)
        .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.IsLatest)
            .IsRequired();

        builder.Property(s => s.CapturedAt)
            .IsRequired();

        builder.Property(s => s.SchemaHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(s => new { s.DatabaseConnectionId, s.CapturedAt });
        builder.HasIndex(s => new { s.DatabaseConnectionId, s.IsLatest });
        builder.HasIndex(s => new { s.DatabaseConnectionId, s.SchemaHash });
        builder.HasIndex(s=>s.SchemaText).HasMethod("gin");

        builder.HasOne(s => s.DatabaseConnection)
            .WithMany(c => c.SchemaSnapshots)
            .HasForeignKey(s => s.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
