using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class ChapterTranslationConfiguration : IEntityTypeConfiguration<ChapterTranslation>
{
    public void Configure(EntityTypeBuilder<ChapterTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.ChapterId, t.LanguageCode })
            .IsUnique();

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);
    }
}
