using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class Chapter : BaseEntity
{
    public Guid CourseVersionId { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public bool HasChildren { get; set; } = false;

    public CourseVersion CourseVersion { get; set; } = default!;
    public Chapter? Parent { get; set; }
    public ICollection<Chapter> Children { get; set; } = new List<Chapter>();
    public ChapterContent? Content { get; set; }
    public ICollection<ChapterTranslation> Translations { get; set; } = new List<ChapterTranslation>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<UserChapterProgress> UserProgresses { get; set; } = new List<UserChapterProgress>();
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
}
