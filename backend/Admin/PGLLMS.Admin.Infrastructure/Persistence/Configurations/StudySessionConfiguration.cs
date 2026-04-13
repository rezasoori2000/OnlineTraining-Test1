using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(s => new { s.UserId, s.ChapterId });

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
