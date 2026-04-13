namespace PGLLMS.Admin.Domain.Entities;

public class ChapterTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChapterId { get; set; }
    public string LanguageCode { get; set; } = default!;
    public string Title { get; set; } = default!;

    public Chapter Chapter { get; set; } = default!;
}
