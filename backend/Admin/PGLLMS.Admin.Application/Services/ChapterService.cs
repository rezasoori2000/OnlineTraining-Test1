using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Chapter;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Services;

public class ChapterService
{
    private readonly IChapterRepository _chapterRepository;
    private readonly IChapterContentRepository _contentRepository;
    private readonly ICourseVersionRepository _versionRepository;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public ChapterService(
        IChapterRepository chapterRepository,
        IChapterContentRepository contentRepository,
        ICourseVersionRepository versionRepository,
        IHtmlSanitizer htmlSanitizer)
    {
        _chapterRepository = chapterRepository;
        _contentRepository = contentRepository;
        _versionRepository = versionRepository;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<ServiceResult<ChapterResponse>> AddChapterAsync(
        AddChapterRequest request,
        CancellationToken ct = default)
    {
        var version = await _versionRepository.GetByIdAsync(request.CourseVersionId, ct);
        if (version is null)
            return ServiceResult<ChapterResponse>.Failure("Course version not found.");

        if (request.ParentId.HasValue)
        {
            var parent = await _chapterRepository.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null)
                return ServiceResult<ChapterResponse>.Failure("Parent chapter not found.");

            // A parent that already has content cannot have children
            var existingContent = await _contentRepository.GetByChapterIdAsync(parent.Id, ct);
            if (existingContent is not null)
                return ServiceResult<ChapterResponse>.Failure(
                    "Cannot add a child chapter to a chapter that already has content.");

            // Mark parent as having children
            parent.HasChildren = true;
            await _chapterRepository.SaveChangesAsync(ct);
        }

        var order = await _chapterRepository.GetNextOrderAsync(request.CourseVersionId, request.ParentId, ct);

        var chapter = new Chapter
        {
            CourseVersionId = request.CourseVersionId,
            ParentId = request.ParentId,
            Order = order,
            HasChildren = false
        };

        chapter.Translations.Add(new ChapterTranslation
        {
            ChapterId = chapter.Id,
            LanguageCode = request.LanguageCode,
            Title = request.Title
        });

        await _chapterRepository.AddAsync(chapter, ct);
        await _chapterRepository.SaveChangesAsync(ct);

        return ServiceResult<ChapterResponse>.Success(MapToResponse(chapter));
    }

    public async Task<ServiceResult<ChapterContentResponse>> UploadContentAsync(
        UploadChapterContentRequest request,
        CancellationToken ct = default)
    {
        var chapter = await _chapterRepository.GetByIdAsync(request.ChapterId, ct);
        if (chapter is null)
            return ServiceResult<ChapterContentResponse>.Failure("Chapter not found.");

        if (chapter.HasChildren)
            return ServiceResult<ChapterContentResponse>.Failure(
                "Cannot add content to a chapter that has children.");

        var existingContent = await _contentRepository.GetByChapterIdAsync(request.ChapterId, ct);
        if (existingContent is not null)
            return ServiceResult<ChapterContentResponse>.Failure(
                "Chapter already has content. Update the existing content instead.");

        var sanitizedHtml = _htmlSanitizer.Sanitize(request.HtmlContent);

        var content = new ChapterContent
        {
            ChapterId = request.ChapterId,
            HtmlContent = sanitizedHtml
        };

        await _contentRepository.AddAsync(content, ct);
        await _contentRepository.SaveChangesAsync(ct);

        return ServiceResult<ChapterContentResponse>.Success(MapContentToResponse(content));
    }

    private static ChapterResponse MapToResponse(Chapter c) => new()
    {
        Id = c.Id,
        CourseVersionId = c.CourseVersionId,
        ParentId = c.ParentId,
        Order = c.Order,
        HasChildren = c.HasChildren
    };

    private static ChapterContentResponse MapContentToResponse(ChapterContent cc) => new()
    {
        Id = cc.Id,
        ChapterId = cc.ChapterId,
        HtmlContent = cc.HtmlContent,
        CreatedAt = cc.CreatedAt
    };
}
