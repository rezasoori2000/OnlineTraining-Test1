using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Course;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Application.Services;

/// <summary>
/// Handles POST /api/courses/full-create.
/// Creates a course, its translation, the initial version, and the full chapter tree
/// (with content and optional quizzes) all inside a single EF Core SaveChangesAsync call.
/// </summary>
public class FullCourseCreationService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseVersionRepository _versionRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IChapterContentRepository _contentRepository;
    private readonly IQuizRepository _quizRepository;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public FullCourseCreationService(
        ICourseRepository courseRepository,
        ICourseVersionRepository versionRepository,
        IChapterRepository chapterRepository,
        IChapterContentRepository contentRepository,
        IQuizRepository quizRepository,
        IHtmlSanitizer htmlSanitizer)
    {
        _courseRepository = courseRepository;
        _versionRepository = versionRepository;
        _chapterRepository = chapterRepository;
        _contentRepository = contentRepository;
        _quizRepository = quizRepository;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<ServiceResult<FullCreateCourseResponse>> CreateAsync(
        FullCreateCourseRequest request,
        CancellationToken ct = default)
    {
        // 1. Validate inputs before touching the database
        var validationError = ValidateRequest(request);
        if (validationError is not null)
            return ServiceResult<FullCreateCourseResponse>.Failure(validationError);

        // 2. Generate a unique slug from the title
        var baseSlug = SlugHelper.GenerateSlug(request.Course.Title);
        var slug = baseSlug;
        var suffix = 1;
        while (await _courseRepository.SlugExistsAsync(slug, ct))
            slug = $"{baseSlug}-{suffix++}";

        // 3. Build Course + primary translation (graph tracked by EF, not saved yet)
        var course = new Course
        {
            Slug = slug,
            Status = CourseStatus.Draft
        };

        course.Translations.Add(new CourseTranslation
        {
            CourseId = course.Id,
            LanguageCode = request.Course.LanguageCode,
            Title = request.Course.Title,
            Description = request.Course.Description
        });

        await _courseRepository.AddAsync(course, ct);

        // 4. Build initial CourseVersion (v1, unpublished)
        var version = new CourseVersion
        {
            CourseId = course.Id,
            VersionNumber = 1,
            IsPublished = false
        };

        await _versionRepository.AddAsync(version, ct);

        // 5. Recursively build the chapter tree (all tracked, not saved yet)
        await SaveChaptersAsync(
            request.Chapters,
            version.Id,
            parentId: null,
            request.Course.LanguageCode,
            ct);

        // 6. Single SaveChangesAsync — EF wraps everything in one DB transaction
        await _courseRepository.SaveChangesAsync(ct);

        return ServiceResult<FullCreateCourseResponse>.Success(new FullCreateCourseResponse
        {
            CourseId = course.Id,
            Slug = course.Slug,
            CourseVersionId = version.Id
        });
    }

    // ── Recursive chapter builder ─────────────────────────────────────────────

    private async Task SaveChaptersAsync(
        List<FullCreateChapterDto> chapters,
        Guid courseVersionId,
        Guid? parentId,
        string languageCode,
        CancellationToken ct)
    {
        foreach (var dto in chapters)
        {
            var hasChildren = dto.Children.Count > 0;

            // Build chapter entity
            var chapter = new Chapter
            {
                CourseVersionId = courseVersionId,
                ParentId = parentId,
                Order = dto.Order,
                HasChildren = hasChildren
            };

            // English title stored as translation
            chapter.Translations.Add(new ChapterTranslation
            {
                ChapterId = chapter.Id,
                LanguageCode = languageCode,
                Title = dto.Title
            });

            await _chapterRepository.AddAsync(chapter, ct);

            if (hasChildren)
            {
                // Non-leaf: recurse into children; no content
                await SaveChaptersAsync(dto.Children, courseVersionId, chapter.Id, languageCode, ct);
            }
            else
            {
                // Leaf: sanitize and attach HTML content
                var sanitizedHtml = _htmlSanitizer.Sanitize(dto.HtmlContent!);
                await _contentRepository.AddAsync(new ChapterContent
                {
                    ChapterId = chapter.Id,
                    HtmlContent = sanitizedHtml
                }, ct);

                // Leaf: attach optional quiz
                if (dto.Quiz is not null)
                    await BuildQuizAsync(dto.Quiz, chapter.Id, languageCode, ct);
            }
        }
    }

    private async Task BuildQuizAsync(
        FullCreateQuizDto dto,
        Guid chapterId,
        string languageCode,
        CancellationToken ct)
    {
        var quiz = new Quiz
        {
            ChapterId = chapterId,
            IsMandatory = dto.IsMandatory,
            PassingPercentage = dto.PassingPercentage,
            MaxAttempts = 3  // sensible default; configurable later
        };

        foreach (var qDto in dto.Questions)
        {
            var question = new Question
            {
                QuizId = quiz.Id,
                Type = QuestionType.SingleChoice,
                Order = qDto.Order
            };

            // Question text stored as a translation
            question.Translations.Add(new QuestionTranslation
            {
                QuestionId = question.Id,
                LanguageCode = languageCode,
                Text = qDto.Text
            });

            foreach (var optDto in qDto.Options)
            {
                question.Options.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    Text = optDto.Text,
                    IsCorrect = optDto.IsCorrect
                });
            }

            quiz.Questions.Add(question);
        }

        await _quizRepository.AddAsync(quiz, ct);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static string? ValidateRequest(FullCreateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Course.LanguageCode))
            return "LanguageCode is required.";

        if (request.Course.LanguageCode.Length > 10)
            return "LanguageCode must not exceed 10 characters.";

        return ValidateChapters(request.Chapters);
    }

    private static string? ValidateChapters(List<FullCreateChapterDto> chapters)
    {
        foreach (var chapter in chapters)
        {
            if (string.IsNullOrWhiteSpace(chapter.Title))
                return "Chapter title cannot be empty.";

            var hasChildren = chapter.Children.Count > 0;

            // A chapter that has children must not carry content
            if (hasChildren && !string.IsNullOrWhiteSpace(chapter.HtmlContent))
                return $"Chapter '{chapter.Title}' has children and cannot have content.";

            // A leaf chapter MUST have content
            if (!hasChildren && string.IsNullOrWhiteSpace(chapter.HtmlContent))
                return $"Chapter '{chapter.Title}' has no children and content is required.";

            // A non-leaf chapter cannot have a quiz
            if (hasChildren && chapter.Quiz is not null)
                return $"Chapter '{chapter.Title}' has children and cannot have a quiz.";

            if (chapter.Quiz is not null)
            {
                var quizError = ValidateQuiz(chapter.Quiz, chapter.Title);
                if (quizError is not null) return quizError;
            }

            var childError = ValidateChapters(chapter.Children);
            if (childError is not null) return childError;
        }

        return null;
    }

    private static string? ValidateQuiz(FullCreateQuizDto quiz, string chapterTitle)
    {
        if (quiz.IsMandatory && (quiz.PassingPercentage < 1 || quiz.PassingPercentage > 100))
            return $"Chapter '{chapterTitle}': PassingPercentage must be 1–100 when IsMandatory is true.";

        if (quiz.Questions.Count == 0)
            return $"Chapter '{chapterTitle}': Quiz must contain at least one question.";

        foreach (var q in quiz.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Text))
                return $"Chapter '{chapterTitle}': Question text cannot be empty.";

            if (q.Options.Count < 2)
                return $"Chapter '{chapterTitle}': Each question must have at least 2 options.";

            if (!q.Options.Any(o => o.IsCorrect))
                return $"Chapter '{chapterTitle}': Each question must have at least one correct answer marked.";
        }

        return null;
    }
}
