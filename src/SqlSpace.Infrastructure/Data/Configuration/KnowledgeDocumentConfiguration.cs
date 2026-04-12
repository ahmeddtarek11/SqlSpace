using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable("KnowledgeDocuments");

        builder.HasKey(d => d.DocumentId);

        builder.Property(d => d.DocumentId)
            .ValueGeneratedNever();

        builder.Property(d => d.ConnectionId)
            .IsRequired();

        builder.Property(d => d.UploadedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.PythonFileId)
            .HasMaxLength(100);

        builder.Property(d => d.ChunksCreated)
            .IsRequired();

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.ProcessedAt);

        builder.Property(d => d.IsDeleted)
            .IsRequired();

        builder.HasOne(d => d.DatabaseConnection)
            .WithMany(c => c.KnowledgeDocuments)
            .HasForeignKey(d => d.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.ConnectionId, d.IsDeleted });
        builder.HasIndex(d => d.Status);
    }
}
