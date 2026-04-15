using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class FolderCourseConfiguration : IEntityTypeConfiguration<FolderCourse>
{
    public void Configure(EntityTypeBuilder<FolderCourse> builder)
    {
        builder.HasKey(fc => new { fc.FolderId, fc.CourseId });

        builder.HasOne(fc => fc.Course)
            .WithMany()
            .HasForeignKey(fc => fc.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
