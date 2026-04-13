using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Course;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Services;

/// <summary>
/// Handles GET /api/courses/{id} — returns the full course detail including
/// the active version's chapter tree, per-leaf content, and quizzes.
/// </summary>
public class CourseDetailService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IChapterContentRepository _contentRepository;

    public CourseDetailService(
        ICourseRepository courseRepository,
        IChapterRepository chapterRepository,
        IChapterContentRepository contentRepository)
    {
        _courseRepository = courseRepository;
        _chapterRepository = chapterRepository;
        _contentRepository = contentRepository;
    }

    public async Task<ServiceResult<CourseDetailDto>> GetAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _courseRepository.GetDetailAsync(courseId, ct);
        if (course is null)
            return ServiceResult<CourseDetailDto>.Failure("Course not found.");

        // Active version = highest version number (prefer draft over published for editing)
        var activeVersion = course.Versions
            .OrderByDescending(v => !v.IsPublished)   // drafts first
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (activeVersion is null)
            return ServiceResult<CourseDetailDto>.Failure("Course has no versions.");

        var primaryTranslation = course.Translations.FirstOrDefault();

        // Load chapter structure (translations + quizzes). HTML content is deliberately NOT loaded here.
        var flatChapters = await _chapterRepository.GetFullByVersionAsync(activeVersion.Id, ct);

        // Lightweight check: which chapters have stored HTML content (no HTML bytes transferred)
        var chapterIds = flatChapters.Select(c => c.Id).ToList();
        var chapterIdsWithContent = await _contentRepository.GetChapterIdsWithContentAsync(chapterIds, ct);

        // Build recursive tree from the flat list
        var rootChapters = BuildTree(flatChapters, parentId: null, chapterIdsWithContent);

        var dto = new CourseDetailDto
        {
            Id = course.Id,
            Slug = course.Slug,
            Status = course.Status.ToString(),
            Title = primaryTranslation?.Title ?? course.Slug,
            Description = primaryTranslation?.Description,
            LanguageCode = primaryTranslation?.LanguageCode ?? "en",
            ActiveVersionId = activeVersion.Id,
            ActiveVersionNumber = activeVersion.VersionNumber,
            IsVersionPublished = activeVersion.IsPublished,
            Chapters = rootChapters,
        };

        return ServiceResult<CourseDetailDto>.Success(dto);
    }

    // ── Tree builder ──────────────────────────────────────────────────────────

    private static List<ChapterDetailDto> BuildTree(List<Chapter> all, Guid? parentId, HashSet<Guid> chapterIdsWithContent)
    {
        return all
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.Order)
            .Select(c => MapChapter(c, all, chapterIdsWithContent))
            .ToList();
    }

    private static ChapterDetailDto MapChapter(Chapter chapter, List<Chapter> all, HashSet<Guid> chapterIdsWithContent)
    {
        var translation = chapter.Translations.FirstOrDefault();
        var quiz = chapter.Quizzes.FirstOrDefault(q => !q.IsDeleted);

        return new ChapterDetailDto
        {
            Id = chapter.Id,
            Title = translation?.Title ?? string.Empty,
            Order = chapter.Order,
            ParentId = chapter.ParentId,
            HasChildren = chapter.HasChildren,
            HasContent = chapterIdsWithContent.Contains(chapter.Id),
            Quiz = quiz is not null ? MapQuiz(quiz) : null,
            Children = BuildTree(all, chapter.Id, chapterIdsWithContent),
        };
    }

    private static QuizDetailDto MapQuiz(Quiz quiz) => new()
    {
        Id = quiz.Id,
        IsMandatory = quiz.IsMandatory,
        PassingPercentage = quiz.PassingPercentage,
        Questions = quiz.Questions
            .Where(q => !q.IsDeleted)
            .OrderBy(q => q.Order)
            .Select(q => new QuestionDetailDto
            {
                Id = q.Id,
                Text = q.Translations.FirstOrDefault()?.Text ?? string.Empty,
                Order = q.Order,
                Options = q.Options
                    .Where(o => !o.IsDeleted)
                    .Select(o => new OptionDetailDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                    })
                    .ToList(),
            })
            .ToList(),
    };
}
