using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Application.DTOs.Course;

public class CourseResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = default!;
    public CourseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CourseListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}
