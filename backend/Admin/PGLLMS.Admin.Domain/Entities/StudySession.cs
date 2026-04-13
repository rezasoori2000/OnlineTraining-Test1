using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class StudySession : BaseEntity
{
    public string UserId { get; set; } = default!;
    public Guid ChapterId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Duration { get; set; } // seconds

    public Chapter Chapter { get; set; } = default!;
}
