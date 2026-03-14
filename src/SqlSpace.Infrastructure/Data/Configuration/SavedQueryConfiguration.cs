using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class SavedQueryConfiguration : IEntityTypeConfiguration<SavedQuery>
{
    public void Configure(EntityTypeBuilder<SavedQuery> builder)
    {
        builder.ToTable("SavedQueries");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .ValueGeneratedNever();

        builder.Property(q => q.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(q => q.DatabaseConnectionId)
            .IsRequired();

        builder.Property(q => q.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(q => q.UserPrompt)
            .IsRequired();

        builder.Property(q => q.GeneratedSql)
            .IsRequired();

        builder.Property(q => q.QueryHistoryId);

        builder.Property(q => q.CreatedAtUtc)
            .IsRequired();

        builder.Property(q => q.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(q => new { q.UserId, q.CreatedAtUtc });
        builder.HasIndex(q => q.DatabaseConnectionId);

        builder.HasOne(q => q.DatabaseConnection)
            .WithMany()
            .HasForeignKey(q => q.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
