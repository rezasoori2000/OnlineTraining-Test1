using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Course;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Application.Services;

/// <summary>
/// Handles PUT /api/courses/{id}.
///
/// Version-safety rule:
///   • If the active version is Published  → create a new draft version and apply all changes there.
///   • If the active version is Draft      → update it in place.
///
/// All DB changes execute inside a single SaveChangesAsync call (EF change-tracking transaction).
/// </summary>
public class CourseUpdateService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseVersionRepository _versionRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IChapterContentRepository _contentRepository;
    private readonly IQuizRepository _quizRepository;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly CourseDetailService _courseDetailService;

    public CourseUpdateService(
        ICourseRepository courseRepository,
        ICourseVersionRepository versionRepository,
        IChapterRepository chapterRepository,
        IChapterContentRepository contentRepository,
        IQuizRepository quizRepository,
        IHtmlSanitizer htmlSanitizer,
        CourseDetailService courseDetailService)
    {
        _courseRepository = courseRepository;
        _versionRepository = versionRepository;
        _chapterRepository = chapterRepository;
        _contentRepository = contentRepository;
        _quizRepository = quizRepository;
        _htmlSanitizer = htmlSanitizer;
        _courseDetailService = courseDetailService;
    }

    public async Task<ServiceResult<CourseDetailDto>> UpdateAsync(
        Guid courseId,
        UpdateCourseRequest request,
        CancellationToken ct = default)
    {
        // ── 1. Validate ───────────────────────────────────────────────────────
        var validationError = Validate(request);
        if (validationError is not null)
            return ServiceResult<CourseDetailDto>.Failure(validationError);

        // ── 2. Load course + active version ───────────────────────────────────
        var course = await _courseRepository.GetDetailAsync(courseId, ct);
        if (course is null)
            return ServiceResult<CourseDetailDto>.Failure("Course not found.");

        var activeVersion = course.Versions
            .OrderByDescending(v => !v.IsPublished)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (activeVersion is null)
            return ServiceResult<CourseDetailDto>.Failure("Course has no versions.");

        // ── 3. Version-safety: if published, create new draft version ─────────
        CourseVersion workingVersion;
        bool isNewVersion = false;

        if (activeVersion.IsPublished)
        {
            var nextNumber = await _versionRepository.GetNextVersionNumberAsync(courseId, ct);
            workingVersion = new CourseVersion
            {
                CourseId = courseId,
                VersionNumber = nextNumber,
                IsPublished = false,
            };
            await _versionRepository.AddAsync(workingVersion, ct);
            isNewVersion = true;
        }
        else
        {
            workingVersion = activeVersion;
        }

        // ── 4. Update course translation (title / description) ────────────────
        var translation = course.Translations
            .FirstOrDefault(t => t.LanguageCode == request.CourseInfo.LanguageCode)
            ?? course.Translations.FirstOrDefault();

        if (translation is not null)
        {
            translation.Title = request.CourseInfo.Title;
            translation.Description = request.CourseInfo.Description;
        }
        else
        {
            course.Translations.Add(new CourseTranslation
            {
                CourseId = courseId,
                LanguageCode = request.CourseInfo.LanguageCode,
                Title = request.CourseInfo.Title,
                Description = request.CourseInfo.Description,
            });
        }

        // ── 5. Soft-delete removed chapters ───────────────────────────────────
        foreach (var id in request.DeletedNodeIds)
            await _chapterRepository.SoftDeleteWithDescendantsAsync(id, ct);

        // ── 6. Update existing chapters (title / order) ───────────────────────
        foreach (var dto in request.UpdatedNodes)
        {
            var chapter = await _chapterRepository.GetByIdAsync(dto.Id, ct);
            if (chapter is null || chapter.CourseVersionId != workingVersion.Id) continue;

            var chTranslation = chapter.Translations.FirstOrDefault();
            if (chTranslation is not null)
                chTranslation.Title = dto.Title;
            else
                chapter.Translations.Add(new ChapterTranslation
                {
                    ChapterId = chapter.Id,
                    LanguageCode = request.CourseInfo.LanguageCode,
                    Title = dto.Title,
                });

            chapter.Order = dto.Order;
        }

        // ── 7. Insert new chapters ────────────────────────────────────────────
        // clientId → real Guid  (needed to resolve parent references between new nodes)
        var clientToReal = new Dictionary<string, Guid>();

        // Sort: root nodes (no parent) first, children after — guarantees parents are inserted before children
        var ordered = TopologicalSort(request.NewNodes);

        foreach (var dto in ordered)
        {
            Guid? parentId = ResolveParent(dto.ParentRef, clientToReal);

            bool hasChildren = false; // new nodes default to leaf; promote when children are added
            var chapter = new Chapter
            {
                CourseVersionId = workingVersion.Id,
                ParentId = parentId,
                Order = dto.Order,
                HasChildren = hasChildren,
            };

            chapter.Translations.Add(new ChapterTranslation
            {
                ChapterId = chapter.Id,
                LanguageCode = request.CourseInfo.LanguageCode,
                Title = dto.Title,
            });

            await _chapterRepository.AddAsync(chapter, ct);
            clientToReal[dto.ClientId] = chapter.Id;

            // Mark parent as HasChildren if applicable
            if (parentId.HasValue)
                await MarkHasChildrenAsync(parentId.Value, ct);

            // Leaf: attach content if provided
            if (!string.IsNullOrWhiteSpace(dto.HtmlContent))
            {
                var sanitized = _htmlSanitizer.Sanitize(dto.HtmlContent);
                await _contentRepository.AddAsync(new ChapterContent
                {
                    ChapterId = chapter.Id,
                    HtmlContent = sanitized,
                }, ct);
            }

            // Leaf: attach quiz if provided
            if (dto.Quiz is not null)
                await AddQuizAsync(chapter.Id, dto.Quiz, request.CourseInfo.LanguageCode, ct);
        }

        // ── 8. Update existing chapter content ────────────────────────────────
        foreach (var dto in request.UpdatedContents)
        {
            var content = await _contentRepository.GetByChapterIdAsync(dto.ChapterId, ct);
            var sanitized = _htmlSanitizer.Sanitize(dto.HtmlContent);

            if (content is not null)
                await _contentRepository.UpdateAsync(content, sanitized, ct);
            else
                await _contentRepository.AddAsync(new ChapterContent
                {
                    ChapterId = dto.ChapterId,
                    HtmlContent = sanitized,
                }, ct);
        }

        // ── 9. Upsert quizzes ─────────────────────────────────────────────────
        foreach (var dto in request.UpdatedQuizzes)
        {
            // Delete the old quiz entirely (simpler than diffing questions/options)
            await _quizRepository.DeleteByChapterIdAsync(dto.ChapterId, ct);

            var quiz = BuildQuiz(dto.ChapterId, dto, request.CourseInfo.LanguageCode);
            await _quizRepository.AddAsync(quiz, ct);
        }

        // ── 10. Single SaveChangesAsync ───────────────────────────────────────
        await _courseRepository.SaveChangesAsync(ct);

        // ── 11. Return fresh detail via CourseDetailService re-fetch ─────────
        var getResult = await _courseDetailService.GetAsync(courseId, ct);
        if (!getResult.Succeeded)
            return getResult;

        if (isNewVersion)
            getResult.Data!.Status = "NewVersionCreated";

        return getResult;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task MarkHasChildrenAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await _chapterRepository.GetByIdAsync(parentId, ct);
        if (parent is not null)
            parent.HasChildren = true;
    }

    private static Guid? ResolveParent(string? parentRef, Dictionary<string, Guid> clientToReal)
    {
        if (string.IsNullOrEmpty(parentRef)) return null;
        if (clientToReal.TryGetValue(parentRef, out var resolved)) return resolved;
        if (Guid.TryParse(parentRef, out var guid)) return guid;
        return null;
    }

    /// <summary>Sort new nodes so parents come before children.</summary>
    private static List<NewNodeDto> TopologicalSort(List<NewNodeDto> nodes)
    {
        var clientIds = nodes.Select(n => n.ClientId).ToHashSet();
        var result = new List<NewNodeDto>();
        var visited = new HashSet<string>();

        void Visit(NewNodeDto node)
        {
            if (visited.Contains(node.ClientId)) return;
            // If parent is also a new node, visit it first
            if (node.ParentRef is not null && clientIds.Contains(node.ParentRef))
            {
                var parent = nodes.First(n => n.ClientId == node.ParentRef);
                Visit(parent);
            }
            visited.Add(node.ClientId);
            result.Add(node);
        }

        foreach (var n in nodes)
            Visit(n);

        return result;
    }

    private async Task AddQuizAsync(Guid chapterId, NewQuizDto dto, string langCode, CancellationToken ct)
    {
        var quiz = new Quiz
        {
            ChapterId = chapterId,
            IsMandatory = dto.IsMandatory,
            PassingPercentage = dto.PassingPercentage,
            MaxAttempts = 3,
        };
        BuildQuestions(quiz, dto.Questions, langCode);
        await _quizRepository.AddAsync(quiz, ct);
    }

    private static Quiz BuildQuiz(Guid chapterId, UpdatedQuizDto dto, string langCode)
    {
        var quiz = new Quiz
        {
            ChapterId = chapterId,
            IsMandatory = dto.IsMandatory,
            PassingPercentage = dto.PassingPercentage,
            MaxAttempts = 3,
        };
        BuildQuestions(quiz, dto.Questions, langCode);
        return quiz;
    }

    private static void BuildQuestions(Quiz quiz, List<UpsertQuestionDto> questions, string langCode)
    {
        foreach (var qDto in questions)
        {
            var question = new Question
            {
                QuizId = quiz.Id,
                Type = QuestionType.SingleChoice,
                Order = qDto.Order,
            };
            question.Translations.Add(new QuestionTranslation
            {
                QuestionId = question.Id,
                LanguageCode = langCode,
                Text = qDto.Text,
            });
            foreach (var oDto in qDto.Options)
            {
                question.Options.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    Text = oDto.Text,
                    IsCorrect = oDto.IsCorrect,
                });
            }
            quiz.Questions.Add(question);
        }
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static string? Validate(UpdateCourseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CourseInfo?.Title))
            return "Course title is required.";

        foreach (var node in req.NewNodes)
        {
            if (string.IsNullOrWhiteSpace(node.Title))
                return "Chapter title cannot be empty.";

            bool isLeaf = !req.NewNodes.Any(n => n.ParentRef == node.ClientId);
            if (isLeaf && string.IsNullOrWhiteSpace(node.HtmlContent))
                return $"Leaf chapter '{node.Title}' must have content.";

            if (!isLeaf && !string.IsNullOrWhiteSpace(node.HtmlContent))
                return $"Chapter '{node.Title}' has children and must not have content.";

            if (node.Quiz is not null)
            {
                var err = ValidateQuiz(node.Quiz.Questions, node.Title);
                if (err is not null) return err;
            }
        }

        foreach (var quiz in req.UpdatedQuizzes)
        {
            var err = ValidateQuiz(quiz.Questions, $"chapter {quiz.ChapterId}");
            if (err is not null) return err;
        }

        return null;
    }

    private static string? ValidateQuiz(List<UpsertQuestionDto> questions, string context)
    {
        if (questions.Count == 0)
            return $"Quiz for {context} must have at least one question.";

        foreach (var q in questions)
        {
            if (string.IsNullOrWhiteSpace(q.Text))
                return $"Question text cannot be empty (quiz for {context}).";
            if (q.Options.Count < 2)
                return $"Each question must have at least 2 options (quiz for {context}).";
            if (!q.Options.Any(o => o.IsCorrect))
                return $"Each question must have at least one correct answer (quiz for {context}).";
        }
        return null;
    }
}
