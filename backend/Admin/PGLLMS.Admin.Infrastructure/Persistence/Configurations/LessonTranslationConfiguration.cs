using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class LessonTranslationConfiguration : IEntityTypeConfiguration<LessonTranslation>
{
    public void Configure(EntityTypeBuilder<LessonTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.LessonId, t.LanguageCode })
            .IsUnique();

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);
    }
}
