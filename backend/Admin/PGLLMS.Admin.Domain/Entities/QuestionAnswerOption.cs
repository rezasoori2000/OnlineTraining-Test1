namespace PGLLMS.Admin.Domain.Entities;

/// <summary>
/// Join table between QuestionAnswer and QuestionOption (selected options).
/// </summary>
public class QuestionAnswerOption
{
    public Guid QuestionAnswerId { get; set; }
    public Guid QuestionOptionId { get; set; }

    public QuestionAnswer QuestionAnswer { get; set; } = default!;
    public QuestionOption QuestionOption { get; set; } = default!;
}
