using PGLLMS.Admin.Domain.Common;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Domain.Entities;

public class Lesson : BaseEntity
{
    public string Slug { get; set; } = default!;
    public LessonStatus Status { get; set; } = LessonStatus.Draft;

    public ICollection<LessonVersion> Versions { get; set; } = new List<LessonVersion>();
    public ICollection<LessonTranslation> Translations { get; set; } = new List<LessonTranslation>();
    public ICollection<LessonTag> LessonTags { get; set; } = new List<LessonTag>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
