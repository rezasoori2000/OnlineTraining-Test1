using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Text)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
