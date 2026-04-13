using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Application.DTOs.Lesson;

public class LessonResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = default!;
    public LessonStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
