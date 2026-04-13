using PGLLMS.Admin.Domain.Common;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Domain.Entities;

public class Course : BaseEntity
{
    public string Slug { get; set; } = default!;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    public ICollection<CourseVersion> Versions { get; set; } = new List<CourseVersion>();
    public ICollection<CourseTranslation> Translations { get; set; } = new List<CourseTranslation>();
    public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
