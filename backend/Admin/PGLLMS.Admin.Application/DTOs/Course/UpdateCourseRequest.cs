namespace PGLLMS.Admin.Application.DTOs.Course;

// ── PUT /api/courses/{id} request — change-set based ─────────────────────────

public class UpdateCourseRequest
{
    /// <summary>Updated course-level info (title, description).</summary>
    public UpdateCourseInfoDto CourseInfo { get; set; } = default!;

    /// <summary>Chapters that already exist and have been modified (title or order changed).</summary>
    public List<UpdatedNodeDto> UpdatedNodes { get; set; } = new();

    /// <summary>Brand-new chapters to insert. ClientId is a frontend-generated UUID used to resolve parentage.</summary>
    public List<NewNodeDto> NewNodes { get; set; } = new();

    /// <summary>IDs of existing chapters to soft-delete (along with their subtree).</summary>
    public List<Guid> DeletedNodeIds { get; set; } = new();

    /// <summary>Content replacements for leaf chapters that already exist.</summary>
    public List<UpdatedContentDto> UpdatedContents { get; set; } = new();

    /// <summary>Quiz upserts — keyed by the real chapter ID.</summary>
    public List<UpdatedQuizDto> UpdatedQuizzes { get; set; } = new();
}

public class UpdateCourseInfoDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string LanguageCode { get; set; } = "en";
}

public class UpdatedNodeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public int Order { get; set; }
}

public class NewNodeDto
{
    /// <summary>Frontend-generated UUID. Used only to resolve parent references from other new nodes.</summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// null  = root chapter.
    /// Guid  = existing parent chapter ID.
    /// string (non-Guid) = ClientId of another new node that is the parent.
    /// </summary>
    public string? ParentRef { get; set; }

    public string Title { get; set; } = default!;
    public int Order { get; set; }
    public string? HtmlContent { get; set; }
    public NewQuizDto? Quiz { get; set; }
}

public class UpdatedContentDto
{
    public Guid ChapterId { get; set; }
    public string HtmlContent { get; set; } = default!;
}

public class UpdatedQuizDto
{
    public Guid ChapterId { get; set; }
    public bool IsMandatory { get; set; }
    public int PassingPercentage { get; set; }
    public List<UpsertQuestionDto> Questions { get; set; } = new();
}

public class NewQuizDto
{
    public bool IsMandatory { get; set; }
    public int PassingPercentage { get; set; }
    public List<UpsertQuestionDto> Questions { get; set; } = new();
}

public class UpsertQuestionDto
{
    /// <summary>null = new question.</summary>
    public Guid? Id { get; set; }
    public string Text { get; set; } = default!;
    public int Order { get; set; }
    public List<UpsertOptionDto> Options { get; set; } = new();
}

public class UpsertOptionDto
{
    /// <summary>null = new option.</summary>
    public Guid? Id { get; set; }
    public string Text { get; set; } = default!;
    public bool IsCorrect { get; set; }
}
