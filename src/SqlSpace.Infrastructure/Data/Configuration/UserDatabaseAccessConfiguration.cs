using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class UserDatabaseAccessConfiguration : IEntityTypeConfiguration<UserDatabaseAccess>
{
    public void Configure(EntityTypeBuilder<UserDatabaseAccess> builder)
    {
        builder.ToTable("UserDatabaseAccesses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.DatabaseConnectionId)
            .IsRequired();

        builder.Property(a => a.HasFullAccess)
            .IsRequired();

        builder.Property(a => a.GrantedAt)
            .IsRequired();

        builder.Property(a => a.GrantedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.IsDeleted)
            .IsRequired();

        builder.Property(a => a.RevokedAt);

        builder.Property(a => a.RevokedByUserId)
            .HasMaxLength(450);

        builder.HasIndex(a => new { a.DatabaseConnectionId, a.UserId, a.IsDeleted, a.RevokedAt });
        builder.HasIndex(a => new { a.UserId, a.IsDeleted });

        builder.HasOne(a => a.DatabaseConnection)
            .WithMany(c => c.UserAccesses)
            .HasForeignKey(a => a.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.TableRestrictions)
            .WithOne(r => r.UserDatabaseAccess)
            .HasForeignKey(r => r.UserDatabaseAccessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
