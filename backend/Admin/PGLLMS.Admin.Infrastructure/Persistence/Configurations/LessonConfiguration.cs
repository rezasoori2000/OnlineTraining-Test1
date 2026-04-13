using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Slug)
            .IsRequired()
            .HasMaxLength(600);

        builder.HasIndex(l => l.Slug)
            .IsUnique();

        builder.Property(l => l.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.HasMany(l => l.Versions)
            .WithOne(v => v.Lesson)
            .HasForeignKey(v => v.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Translations)
            .WithOne(t => t.Lesson)
            .HasForeignKey(t => t.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
