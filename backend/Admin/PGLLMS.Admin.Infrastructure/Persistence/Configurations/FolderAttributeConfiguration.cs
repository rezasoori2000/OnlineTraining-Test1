using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Infrastructure.Persistence.Configurations;

public class FolderAttributeConfiguration : IEntityTypeConfiguration<FolderAttribute>
{
    public void Configure(EntityTypeBuilder<FolderAttribute> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Value)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
