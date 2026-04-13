using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Lesson;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Services;

public class LessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILessonVersionRepository _versionRepository;

    public LessonService(
        ILessonRepository lessonRepository,
        ILessonVersionRepository versionRepository)
    {
        _lessonRepository = lessonRepository;
        _versionRepository = versionRepository;
    }

    public async Task<ServiceResult<LessonResponse>> CreateLessonAsync(
        CreateLessonRequest request,
        CancellationToken ct = default)
    {
        var baseSlug = SlugHelper.GenerateSlug(request.Title);
        var slug = baseSlug;
        var suffix = 1;

        while (await _lessonRepository.SlugExistsAsync(slug, ct))
            slug = $"{baseSlug}-{suffix++}";

        var lesson = new Lesson
        {
            Slug = slug,
            Status = request.Status
        };

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            lesson.Translations.Add(new LessonTranslation
            {
                LessonId = lesson.Id,
                LanguageCode = request.LanguageCode ?? "en",
                Title = request.Title,
                Description = request.Description
            });
        }

        await _lessonRepository.AddAsync(lesson, ct);
        await _lessonRepository.SaveChangesAsync(ct);

        return ServiceResult<LessonResponse>.Success(MapToResponse(lesson));
    }

    public async Task<ServiceResult<LessonVersionResponse>> CreateLessonVersionAsync(
        CreateLessonVersionRequest request,
        CancellationToken ct = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(request.LessonId, ct);
        if (lesson is null)
            return ServiceResult<LessonVersionResponse>.Failure("Lesson not found.");

        var nextNumber = await _versionRepository.GetNextVersionNumberAsync(request.LessonId, ct);

        var version = new LessonVersion
        {
            LessonId = request.LessonId,
            VersionNumber = nextNumber,
            IsPublished = false
        };

        await _versionRepository.AddAsync(version, ct);
        await _versionRepository.SaveChangesAsync(ct);

        return ServiceResult<LessonVersionResponse>.Success(MapVersionToResponse(version));
    }

    private static LessonResponse MapToResponse(Lesson lesson) => new()
    {
        Id = lesson.Id,
        Slug = lesson.Slug,
        Status = lesson.Status,
        CreatedAt = lesson.CreatedAt,
        UpdatedAt = lesson.UpdatedAt
    };

    private static LessonVersionResponse MapVersionToResponse(LessonVersion v) => new()
    {
        Id = v.Id,
        LessonId = v.LessonId,
        VersionNumber = v.VersionNumber,
        IsPublished = v.IsPublished,
        CreatedAt = v.CreatedAt
    };
}
