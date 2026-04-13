namespace PGLLMS.Admin.Domain.Entities;

public class QuestionTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public string LanguageCode { get; set; } = default!;
    public string Text { get; set; } = default!;

    public Question Question { get; set; } = default!;
}
