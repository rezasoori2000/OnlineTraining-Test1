using PGLLMS.Admin.Domain.Common;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Domain.Entities;

public class Question : BaseEntity
{
    public Guid QuizId { get; set; }
    public QuestionType Type { get; set; }
    public int Order { get; set; }

    public Quiz Quiz { get; set; } = default!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();
    public ICollection<QuestionTranslation> Translations { get; set; } = new List<QuestionTranslation>();
}
