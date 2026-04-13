using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class LessonVersion : BaseEntity
{
    public Guid LessonId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; } = false;

    public Lesson Lesson { get; set; } = default!;
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
