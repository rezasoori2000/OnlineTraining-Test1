namespace PGLLMS.Admin.Domain.Entities;

public class LessonTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public string LanguageCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public Lesson Lesson { get; set; } = default!;
}
