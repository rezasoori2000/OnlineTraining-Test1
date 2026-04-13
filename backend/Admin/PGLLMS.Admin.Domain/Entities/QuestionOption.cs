using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = default!;
    public bool IsCorrect { get; set; }

    public Question Question { get; set; } = default!;
    public ICollection<QuestionAnswerOption> AnswerOptions { get; set; } = new List<QuestionAnswerOption>();
}
