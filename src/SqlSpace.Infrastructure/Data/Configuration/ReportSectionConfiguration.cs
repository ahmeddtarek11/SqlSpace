using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class ReportSectionConfiguration : IEntityTypeConfiguration<ReportSection>
{
    public void Configure(EntityTypeBuilder<ReportSection> builder)
    {
        builder.ToTable("ReportSections");

        builder.HasKey(s => s.SectionId);
        builder.Property(s => s.SectionId).ValueGeneratedNever();

        builder.Property(s => s.ReportId).IsRequired();

        builder.Property(s => s.SortOrder).IsRequired();

        builder.Property(s => s.Heading)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.NarrativeText)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(s => s.ChartType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.ChartConfigJson);

        builder.Property(s => s.SqlQuery)
            .HasColumnType("text");

        builder.Property(s => s.CachedResultsJson)
            .HasColumnType("jsonb");

        builder.Property(s => s.CachedResultsErrorMessage)
            .HasMaxLength(2000);
    }
}
