using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Score { get; set; }
    public bool Passed { get; set; }
    public int AttemptNumber { get; set; }

    public Quiz Quiz { get; set; } = default!;
}
