using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Course;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Application.Services;

public class CourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseVersionRepository _versionRepository;

    public CourseService(
        ICourseRepository courseRepository,
        ICourseVersionRepository versionRepository)
    {
        _courseRepository = courseRepository;
        _versionRepository = versionRepository;
    }

    public async Task<List<CourseListItemDto>> GetAllCoursesAsync(CancellationToken ct = default)
    {
        var courses = await _courseRepository.GetAllAsync(ct);
        return courses.Select(c => new CourseListItemDto
        {
            Id = c.Id,
            Title = c.Translations.FirstOrDefault()?.Title ?? c.Slug,
            Slug = c.Slug,
            Status = c.Status.ToString(),
            Version = c.Versions.Count > 0 ? c.Versions.Max(v => v.VersionNumber) : 0,
            UpdatedAt = c.UpdatedAt,
        }).ToList();
    }

    public async Task<ServiceResult<CourseResponse>> CreateCourseAsync(
        CreateCourseRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.LanguageCode))
            return ServiceResult<CourseResponse>.Failure("LanguageCode is required.");

        // Generate unique slug
        var baseSlug = SlugHelper.GenerateSlug(request.Title);
        var slug = baseSlug;
        var suffix = 1;

        while (await _courseRepository.SlugExistsAsync(slug, ct))
            slug = $"{baseSlug}-{suffix++}";

        // Create Course (Status = Draft)
        var course = new Course
        {
            Slug = slug,
            Status = CourseStatus.Draft
        };

        // Create CourseTranslation
        course.Translations.Add(new CourseTranslation
        {
            CourseId = course.Id,
            LanguageCode = request.LanguageCode,
            Title = request.Title,
            Description = request.Description
        });

        // Create initial CourseVersion (VersionNumber = 1, IsPublished = false)
        var version = new CourseVersion
        {
            CourseId = course.Id,
            VersionNumber = 1,
            IsPublished = false
        };

        // Save all atomically — both repos share the same scoped DbContext
        await _courseRepository.AddAsync(course, ct);
        await _versionRepository.AddAsync(version, ct);
        await _courseRepository.SaveChangesAsync(ct);

        return ServiceResult<CourseResponse>.Success(MapToResponse(course));
    }

    private static CourseResponse MapToResponse(Course course) => new()
    {
        Id = course.Id,
        Slug = course.Slug,
        Status = course.Status,
        CreatedAt = course.CreatedAt,
        UpdatedAt = course.UpdatedAt
    };
}
