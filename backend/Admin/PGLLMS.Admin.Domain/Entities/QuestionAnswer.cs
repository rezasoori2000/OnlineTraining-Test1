using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

/// <summary>
/// Stores a user's answer to a question.
/// For descriptive questions: AnswerText is populated.
/// For choice questions: AnswerOptions relation is used.
/// </summary>
public class QuestionAnswer : BaseEntity
{
    public Guid QuestionId { get; set; }
    public string UserId { get; set; } = default!;
    public string? AnswerText { get; set; }
    public bool IsCorrect { get; set; }

    public Question Question { get; set; } = default!;
    public ICollection<QuestionAnswerOption> SelectedOptions { get; set; } = new List<QuestionAnswerOption>();
}
