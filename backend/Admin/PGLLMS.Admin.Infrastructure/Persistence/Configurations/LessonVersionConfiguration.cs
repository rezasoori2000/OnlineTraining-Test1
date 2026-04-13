using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class LessonVersionConfiguration : IEntityTypeConfiguration<LessonVersion>
{
    public void Configure(EntityTypeBuilder<LessonVersion> builder)
    {
        builder.HasKey(v => v.Id);

        // Only one published version per lesson
        builder.HasIndex(v => new { v.LessonId, v.IsPublished })
            .IsUnique()
            .HasFilter("[IsPublished] = 1");

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
