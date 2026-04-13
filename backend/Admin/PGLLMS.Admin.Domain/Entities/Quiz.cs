using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class Quiz : BaseEntity
{
    public Guid ChapterId { get; set; }
    public bool IsMandatory { get; set; }
    public int PassingPercentage { get; set; }
    public int MaxAttempts { get; set; }

    public Chapter Chapter { get; set; } = default!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
