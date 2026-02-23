using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class QueryHistoryConfiguration : IEntityTypeConfiguration<QueryHistory>
{
    public void Configure(EntityTypeBuilder<QueryHistory> builder)
    {
        builder.ToTable("QueryHistories");

        builder.HasKey(q => q.QueryId);

        builder.Property(q => q.QueryId)
            .ValueGeneratedNever();

        builder.Property(q => q.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(q => q.DatabaseConnectionId)
            .IsRequired();

        builder.Property(q => q.UserPrompt)
            .IsRequired();

        builder.Property(q => q.GeneratedSql)
            .IsRequired();

        builder.Property(q => q.LlmResponse);

        builder.Property(q => q.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(q => q.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(q => q.ResultsJson).HasColumnType("jsonb");

        builder.Property(q => q.RowsReturned);

        builder.Property(q => q.ExecutionTimeMs);

        builder.Property(q => q.ExecutedAt)
            .IsRequired();

        builder.Property(q => q.AccessibleTablesSnapshot).HasColumnType("jsonb");

        builder.Property(q => q.RestrictedTablesSnapshot).HasColumnType("jsonb");

        builder.Property(q => q.WasAdminAtExecution)
            .IsRequired();

        builder.HasIndex(q => new { q.DatabaseConnectionId, q.ExecutedAt });
        builder.HasIndex(q => new { q.UserId, q.ExecutedAt });
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.ResultsJson)
        .HasMethod("gin");

        builder.HasOne(q => q.DatabaseConnection)
            .WithMany(c => c.Queries)
            .HasForeignKey(q => q.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
