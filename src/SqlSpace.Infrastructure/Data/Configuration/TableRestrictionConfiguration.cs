// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
// using SqlSpace.Domain.Models;

// namespace SqlSpace.Infrastructure.Data.Configuration;

// public class TableRestrictionConfiguration : IEntityTypeConfiguration<TableRestriction>
// {
//     public void Configure(EntityTypeBuilder<TableRestriction> builder)
//     {
//         builder.ToTable("TableRestrictions");

//         builder.HasKey(r => r.Id);

//         builder.Property(r => r.UserDatabaseAccessId)
//             .IsRequired();

//         builder.Property(r => r.TableName)
//             .IsRequired()
//             .HasMaxLength(256);

//         builder.Property(r => r.SchemaName)
//             .HasMaxLength(128);

//         builder.Property(r => r.CreatedAt)
//             .IsRequired();

//         builder.HasIndex(r => r.UserDatabaseAccessId);
//         builder.HasIndex(r => new { r.UserDatabaseAccessId, r.SchemaName, r.TableName });

//         builder.HasOne(r => r.UserDatabaseAccess)
//             .WithMany(a => a.TableRestrictions)
//             .HasForeignKey(r => r.UserDatabaseAccessId)
//             .OnDelete(DeleteBehavior.Restrict);
//     }
// }
