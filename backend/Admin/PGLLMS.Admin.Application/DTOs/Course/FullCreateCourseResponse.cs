namespace PGLLMS.Admin.Application.DTOs.Course;

public class FullCreateCourseResponse
{
    public Guid CourseId { get; set; }
    public string Slug { get; set; } = default!;
    public Guid CourseVersionId { get; set; }
}
