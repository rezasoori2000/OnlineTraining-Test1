using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(600);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasMany(c => c.Versions)
            .WithOne(v => v.Course)
            .HasForeignKey(v => v.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Translations)
            .WithOne(t => t.Course)
            .HasForeignKey(t => t.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Attachments)
            .WithOne(a => a.Course)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
