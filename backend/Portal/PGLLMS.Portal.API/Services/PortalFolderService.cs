using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Portal.API.DTOs;

namespace PGLLMS.Portal.API.Services;

public class PortalFolderService
{
    private readonly IFolderRepository _folderRepo;

    public PortalFolderService(IFolderRepository folderRepo)
    {
        _folderRepo = folderRepo;
    }

    public async Task<List<PortalFolderTreeNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var folders = await _folderRepo.GetTreeAsync(ct);
        var roots = folders.Where(f => f.ParentId == null).ToList();
        return roots.Select(BuildTreeNode).ToList();
    }

    public async Task<PortalFolderDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await _folderRepo.GetByIdWithDetailsAsync(id, ct);
        if (folder is null) return null;

        var courses = folder.FolderCourses.Select(fc =>
        {
            var title = fc.Course.Translations.FirstOrDefault()?.Title ?? fc.Course.Slug;
            var desc = fc.Course.Translations.FirstOrDefault()?.Description;
            return new PortalFolderCourseDto(fc.CourseId, title, desc);
        }).ToList();

        return new PortalFolderDetailDto(
            folder.Id,
            folder.Name,
            folder.Description,
            folder.HtmlContent,
            courses);
    }

    private static PortalFolderTreeNodeDto BuildTreeNode(Folder f)
    {
        var children = f.Children
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(BuildTreeNode)
            .ToList();

        return new PortalFolderTreeNodeDto(f.Id, f.Name, f.ParentId, children);
    }
}
