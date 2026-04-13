using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class UserChapterProgress : BaseEntity
{
    public string UserId { get; set; } = default!;
    public Guid ChapterId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    public Chapter Chapter { get; set; } = default!;
}
