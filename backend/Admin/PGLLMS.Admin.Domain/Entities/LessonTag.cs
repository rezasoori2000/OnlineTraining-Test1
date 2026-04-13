namespace PGLLMS.Admin.Domain.Entities;

public class LessonTag
{
    public Guid LessonId { get; set; }
    public Guid TagId { get; set; }

    public Lesson Lesson { get; set; } = default!;
    public Tag Tag { get; set; } = default!;
}
