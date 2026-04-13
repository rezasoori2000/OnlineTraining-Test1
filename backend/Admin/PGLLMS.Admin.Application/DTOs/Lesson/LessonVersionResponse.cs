namespace PGLLMS.Admin.Application.DTOs.Lesson;

public class LessonVersionResponse
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
