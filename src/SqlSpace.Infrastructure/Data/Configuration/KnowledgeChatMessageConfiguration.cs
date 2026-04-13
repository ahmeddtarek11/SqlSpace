using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Data.Configuration;

public class KnowledgeChatMessageConfiguration : IEntityTypeConfiguration<KnowledgeChatMessage>
{
    public void Configure(EntityTypeBuilder<KnowledgeChatMessage> builder)
    {
        builder.ToTable("KnowledgeChatMessages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId)
            .ValueGeneratedNever();

        builder.Property(m => m.ConnectionId)
            .IsRequired();

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.SourcesJson)
            .HasColumnType("jsonb");

        builder.Property(m => m.TokensUsed);

        builder.Property(m => m.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasOne(m => m.DatabaseConnection)
            .WithMany()
            .HasForeignKey(m => m.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ConnectionId, m.UserId, m.CreatedAt });
    }
}
