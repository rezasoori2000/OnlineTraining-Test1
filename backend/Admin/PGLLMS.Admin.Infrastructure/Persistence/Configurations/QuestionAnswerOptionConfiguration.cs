using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class QuestionAnswerOptionConfiguration : IEntityTypeConfiguration<QuestionAnswerOption>
{
    public void Configure(EntityTypeBuilder<QuestionAnswerOption> builder)
    {
        builder.HasKey(ao => new { ao.QuestionAnswerId, ao.QuestionOptionId });

        builder.HasOne(ao => ao.QuestionAnswer)
            .WithMany(a => a.SelectedOptions)
            .HasForeignKey(ao => ao.QuestionAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ao => ao.QuestionOption)
            .WithMany(o => o.AnswerOptions)
            .HasForeignKey(ao => ao.QuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
