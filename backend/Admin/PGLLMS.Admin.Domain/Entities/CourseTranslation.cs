namespace PGLLMS.Admin.Domain.Entities;

public class CourseTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public string LanguageCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public Course Course { get; set; } = default!;
}
