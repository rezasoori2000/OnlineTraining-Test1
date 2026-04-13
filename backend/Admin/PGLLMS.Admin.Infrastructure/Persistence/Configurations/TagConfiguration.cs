using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Name)
            .IsUnique();
    }
}

public class CourseTagConfiguration : IEntityTypeConfiguration<CourseTag>
{
    public void Configure(EntityTypeBuilder<CourseTag> builder)
    {
        builder.HasKey(ct => new { ct.CourseId, ct.TagId });

        builder.HasOne(ct => ct.Course)
            .WithMany(c => c.CourseTags)
            .HasForeignKey(ct => ct.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Tag)
            .WithMany(t => t.CourseTags)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LessonTagConfiguration : IEntityTypeConfiguration<LessonTag>
{
    public void Configure(EntityTypeBuilder<LessonTag> builder)
    {
        builder.HasKey(lt => new { lt.LessonId, lt.TagId });

        builder.HasOne(lt => lt.Lesson)
            .WithMany(l => l.LessonTags)
            .HasForeignKey(lt => lt.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lt => lt.Tag)
            .WithMany()
            .HasForeignKey(lt => lt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
