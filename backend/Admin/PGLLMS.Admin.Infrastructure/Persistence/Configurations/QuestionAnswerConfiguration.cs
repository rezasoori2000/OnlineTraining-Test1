using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class QuestionAnswerConfiguration : IEntityTypeConfiguration<QuestionAnswer>
{
    public void Configure(EntityTypeBuilder<QuestionAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.AnswerText)
            .HasMaxLength(4000);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasMany(a => a.SelectedOptions)
            .WithOne(ao => ao.QuestionAnswer)
            .HasForeignKey(ao => ao.QuestionAnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
