namespace PGLLMS.Admin.Application.DTOs.Course;

// ── GET /api/courses/{id} response ────────────────────────────────────────────

public class CourseDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string LanguageCode { get; set; } = "en";

    public Guid ActiveVersionId { get; set; }
    public int ActiveVersionNumber { get; set; }
    public bool IsVersionPublished { get; set; }

    public List<ChapterDetailDto> Chapters { get; set; } = new();
}

public class ChapterDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public int Order { get; set; }
    public Guid? ParentId { get; set; }
    public bool HasChildren { get; set; }

    /// <summary>True when this leaf chapter has stored HTML content. The content itself is NOT returned here — load on demand if needed.</summary>
    public bool HasContent { get; set; }

    public QuizDetailDto? Quiz { get; set; }

    public List<ChapterDetailDto> Children { get; set; } = new();
}

public class QuizDetailDto
{
    public Guid Id { get; set; }
    public bool IsMandatory { get; set; }
    public int PassingPercentage { get; set; }
    public List<QuestionDetailDto> Questions { get; set; } = new();
}

public class QuestionDetailDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = default!;
    public int Order { get; set; }
    public List<OptionDetailDto> Options { get; set; } = new();
}

public class OptionDetailDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = default!;
    public bool IsCorrect { get; set; }
}
