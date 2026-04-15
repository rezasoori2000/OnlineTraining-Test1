using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;
using PGLLMS.Portal.API.DTOs;

namespace PGLLMS.Portal.API.Services;

public class PortalCourseService
{
    private readonly ICourseRepository _courseRepo;
    private readonly ICourseVersionRepository _versionRepo;
    private readonly IChapterRepository _chapterRepo;
    private readonly IChapterContentRepository _contentRepo;
    private readonly AdminDbContext _dbContext;

    public PortalCourseService(
        ICourseRepository courseRepo,
        ICourseVersionRepository versionRepo,
        IChapterRepository chapterRepo,
        IChapterContentRepository contentRepo,
        AdminDbContext dbContext)
    {
        _courseRepo = courseRepo;
        _versionRepo = versionRepo;
        _chapterRepo = chapterRepo;
        _contentRepo = contentRepo;
        _dbContext = dbContext;
    }

    public async Task<PortalCourseDetailDto?> GetCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _courseRepo.GetDetailAsync(courseId, ct);
        if (course is null) return null;

        // Prefer the published version; fall back to the latest version
        var version = await _versionRepo.GetPublishedVersionAsync(courseId, ct)
            ?? course.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (version is null) return null;

        var chapters = await _chapterRepo.GetFullByVersionAsync(version.Id, ct);
        var allIds = chapters.Select(c => c.Id).ToList();
        var withContent = await _contentRepo.GetChapterIdsWithContentAsync(allIds, ct);

        var roots = chapters
            .Where(c => c.ParentId == null && !c.IsDeleted)
            .OrderBy(c => c.Order)
            .ToList();

        var title = course.Translations.FirstOrDefault()?.Title ?? course.Slug;
        var desc = course.Translations.FirstOrDefault()?.Description;

        return new PortalCourseDetailDto(
            course.Id,
            title,
            desc,
            roots.Select(r => BuildChapterTree(r, withContent)).ToList());
    }

    public async Task<PortalChapterContentDto?> GetChapterContentAsync(Guid chapterId, CancellationToken ct = default)
    {
        var chapter = await _dbContext.Chapters
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == chapterId, ct);
        if (chapter is null) return null;

        var content = await _contentRepo.GetByChapterIdAsync(chapterId, ct);
        var title = chapter.Translations.FirstOrDefault()?.Title ?? "Untitled";

        return new PortalChapterContentDto(
            chapterId,
            title,
            content?.HtmlContent);
    }

    private static PortalChapterNodeDto BuildChapterTree(Chapter ch, HashSet<Guid> withContent)
    {
        var children = ch.Children
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Order)
            .Select(c => BuildChapterTree(c, withContent))
            .ToList();

        return new PortalChapterNodeDto(
            ch.Id,
            ch.Translations.FirstOrDefault()?.Title ?? "Untitled",
            ch.Order,
            withContent.Contains(ch.Id),
            children);
    }
}
