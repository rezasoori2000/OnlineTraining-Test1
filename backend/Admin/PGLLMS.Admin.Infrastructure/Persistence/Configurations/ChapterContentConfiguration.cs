using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class ChapterContentConfiguration : IEntityTypeConfiguration<ChapterContent>
{
    public void Configure(EntityTypeBuilder<ChapterContent> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.HtmlContent)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
