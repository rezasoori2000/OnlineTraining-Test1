using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class CourseVersion : BaseEntity
{
    public Guid CourseId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; } = false;

    public Course Course { get; set; } = default!;
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
