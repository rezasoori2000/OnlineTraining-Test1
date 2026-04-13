using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.Score)
            .HasColumnType("decimal(5,2)");

        builder.HasIndex(a => new { a.QuizId, a.UserId, a.AttemptNumber });

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
