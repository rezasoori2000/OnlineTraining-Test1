using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class QuestionTranslationConfiguration : IEntityTypeConfiguration<QuestionTranslation>
{
    public void Configure(EntityTypeBuilder<QuestionTranslation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.QuestionId, t.LanguageCode })
            .IsUnique();

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Text)
            .IsRequired()
            .HasMaxLength(2000);
    }
}
