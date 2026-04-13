using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(2000);
    }
}
