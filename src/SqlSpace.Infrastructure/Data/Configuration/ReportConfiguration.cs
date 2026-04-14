using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.HasKey(r => r.ReportId);
        builder.Property(r => r.ReportId).ValueGeneratedNever();

        builder.Property(r => r.ConnectionId).IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.OriginalPrompt)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.Summary)
            .HasColumnType("text");

        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.UpdatedAtUtc).IsRequired();

        builder.HasOne(r => r.DatabaseConnection)
            .WithMany()
            .HasForeignKey(r => r.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Sections)
            .WithOne(s => s.Report)
            .HasForeignKey(s => s.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.ConnectionId, r.UserId, r.CreatedAtUtc });
    }
}
