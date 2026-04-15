namespace PGLLMS.Admin.Domain.Entities;

public class FolderCourse
{
    public Guid FolderId { get; set; }
    public Guid CourseId { get; set; }

    public Folder Folder { get; set; } = default!;
    public Course Course { get; set; } = default!;
}
