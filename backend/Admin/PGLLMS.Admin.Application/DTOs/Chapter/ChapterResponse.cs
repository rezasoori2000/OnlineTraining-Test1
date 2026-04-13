namespace PGLLMS.Admin.Application.DTOs.Chapter;

public class ChapterResponse
{
    public Guid Id { get; set; }
    public Guid CourseVersionId { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public bool HasChildren { get; set; }
}
