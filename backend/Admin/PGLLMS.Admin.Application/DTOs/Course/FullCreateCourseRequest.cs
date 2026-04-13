using System.ComponentModel.DataAnnotations;

namespace PGLLMS.Admin.Application.DTOs.Course;

/// <summary>
/// Top-level request for POST /api/courses/full-create.
/// Creates course + translation + version + chapters + content + quizzes in one transaction.
/// </summary>
public class FullCreateCourseRequest
{
    [Required]
    public FullCreateCourseInfoDto Course { get; set; } = default!;

    /// <summary>Tree of root-level chapters. Each chapter may contain children recursively.</summary>
    public List<FullCreateChapterDto> Chapters { get; set; } = new();
}

public class FullCreateCourseInfoDto
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = default!;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = default!;

    /// <summary>Defaults to "en". Must be a valid ISO 639-1 language code.</summary>
    [MaxLength(10)]
    public string LanguageCode { get; set; } = "en";
}

public class FullCreateChapterDto
{
    /// <summary>Client-side temporary id — used to correlate content/quiz payloads.</summary>
    [Required]
    public string ClientId { get; set; } = default!;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = default!;

    /// <summary>Zero-based display order within its parent (or at root level).</summary>
    public int Order { get; set; }

    /// <summary>Child sections. If non-empty, this chapter cannot have content.</summary>
    public List<FullCreateChapterDto> Children { get; set; } = new();

    /// <summary>HTML content for leaf chapters (no children). Required when Children is empty.</summary>
    public string? HtmlContent { get; set; }

    /// <summary>Optional quiz for leaf chapters.</summary>
    public FullCreateQuizDto? Quiz { get; set; }
}

public class FullCreateQuizDto
{
    public bool IsMandatory { get; set; }

    /// <summary>Required when IsMandatory = true. Range: 1–100.</summary>
    public int PassingPercentage { get; set; }

    [Required]
    [MinLength(1)]
    public List<FullCreateQuestionDto> Questions { get; set; } = new();
}

public class FullCreateQuestionDto
{
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = default!;

    public int Order { get; set; }

    /// <summary>Must contain at least 2 options with exactly one marked IsCorrect.</summary>
    [Required]
    [MinLength(2)]
    public List<FullCreateOptionDto> Options { get; set; } = new();
}

public class FullCreateOptionDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = default!;

    public bool IsCorrect { get; set; }
}
