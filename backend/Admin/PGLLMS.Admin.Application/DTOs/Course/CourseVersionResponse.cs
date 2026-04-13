namespace PGLLMS.Admin.Application.DTOs.Course;

public class CourseVersionResponse
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
