using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(f => f.Description)
            .HasMaxLength(2000);

        builder.HasQueryFilter(f => !f.IsDeleted);

        builder.HasOne(f => f.Parent)
            .WithMany(f => f.Children)
            .HasForeignKey(f => f.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Attributes)
            .WithOne(a => a.Folder)
            .HasForeignKey(a => a.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.FolderCourses)
            .WithOne(fc => fc.Folder)
            .HasForeignKey(fc => fc.FolderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
