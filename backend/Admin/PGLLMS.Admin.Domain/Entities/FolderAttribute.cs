using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class FolderAttribute : BaseEntity
{
    public Guid FolderId { get; set; }
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;

    public Folder Folder { get; set; } = default!;
}
