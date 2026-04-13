using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.CourseVersionId, c.ParentId, c.Order });

        builder.HasQueryFilter(c => !c.IsDeleted);

        // Self-referencing hierarchy
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-one with content
        builder.HasOne(c => c.Content)
            .WithOne(cc => cc.Chapter)
            .HasForeignKey<ChapterContent>(cc => cc.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Translations)
            .WithOne(t => t.Chapter)
            .HasForeignKey(t => t.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Quizzes)
            .WithOne(q => q.Chapter)
            .HasForeignKey(q => q.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
