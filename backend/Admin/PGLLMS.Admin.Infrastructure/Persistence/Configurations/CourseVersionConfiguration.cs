using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class CourseVersionConfiguration : IEntityTypeConfiguration<CourseVersion>
{
    public void Configure(EntityTypeBuilder<CourseVersion> builder)
    {
        builder.HasKey(v => v.Id);

        // Only one published version per course
        builder.HasIndex(v => new { v.CourseId, v.IsPublished })
            .IsUnique()
            .HasFilter("[IsPublished] = 1");

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.HasMany(v => v.Chapters)
            .WithOne(c => c.CourseVersion)
            .HasForeignKey(c => c.CourseVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
