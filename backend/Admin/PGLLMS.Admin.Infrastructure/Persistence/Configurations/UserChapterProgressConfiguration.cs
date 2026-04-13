using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class UserChapterProgressConfiguration : IEntityTypeConfiguration<UserChapterProgress>
{
    public void Configure(EntityTypeBuilder<UserChapterProgress> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.UserId, p.ChapterId })
            .IsUnique();

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
