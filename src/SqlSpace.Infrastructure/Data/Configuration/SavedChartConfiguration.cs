using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class SavedChartConfiguration : IEntityTypeConfiguration<SavedChart>
{
    public void Configure(EntityTypeBuilder<SavedChart> builder)
    {
        builder.ToTable("SavedCharts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(c => c.DatabaseConnectionId)
            .IsRequired();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.SqlQuery)
            .IsRequired();

        builder.Property(c => c.OriginalPrompt);

        builder.Property(c => c.Insight)
            .HasMaxLength(2000);

        builder.Property(c => c.ChartType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.ChartConfigJson)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(c => new { c.UserId, c.DatabaseConnectionId });
        builder.HasIndex(c => c.DatabaseConnectionId);

        builder.HasOne(c => c.DatabaseConnection)
            .WithMany(d => d.SavedCharts)
            .HasForeignKey(c => c.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
