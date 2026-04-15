using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class Folder : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? HtmlContent { get; set; }
    public Guid? ParentId { get; set; }

    public Folder? Parent { get; set; }
    public ICollection<Folder> Children { get; set; } = new List<Folder>();
    public ICollection<FolderAttribute> Attributes { get; set; } = new List<FolderAttribute>();
    public ICollection<FolderCourse> FolderCourses { get; set; } = new List<FolderCourse>();
}
