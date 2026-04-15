using PGLLMS.Admin.Application.Common;
using PGLLMS.Admin.Application.DTOs.Folder;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Services;

public class FolderService
{
    private readonly IFolderRepository _folderRepository;
    private readonly ICourseRepository _courseRepository;

    public FolderService(IFolderRepository folderRepository, ICourseRepository courseRepository)
    {
        _folderRepository = folderRepository;
        _courseRepository = courseRepository;
    }

    public async Task<List<FolderListItemDto>> GetAllFoldersAsync(CancellationToken ct = default)
    {
        var folders = await _folderRepository.GetAllAsync(ct);
        return folders.Select(f => new FolderListItemDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            ParentName = f.Parent?.Name,
            ChildrenCount = f.Children.Count,
            UpdatedAt = f.UpdatedAt,
        }).ToList();
    }

    public async Task<ServiceResult<FolderDetailDto>> GetFolderAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await _folderRepository.GetByIdWithDetailsAsync(id, ct);
        if (folder is null)
            return ServiceResult<FolderDetailDto>.Failure("Folder not found.");

        return ServiceResult<FolderDetailDto>.Success(new FolderDetailDto
        {
            Id = folder.Id,
            Name = folder.Name,
            Description = folder.Description,
            HtmlContent = folder.HtmlContent,
            ParentId = folder.ParentId,
            ParentName = folder.Parent?.Name,
            Attributes = folder.Attributes.Select(a => new FolderAttributeDto
            {
                Id = a.Id,
                Key = a.Key,
                Value = a.Value,
            }).ToList(),
            Courses = folder.FolderCourses.Select(fc => new FolderCourseDto
            {
                CourseId = fc.CourseId,
                Title = fc.Course.Translations.FirstOrDefault()?.Title ?? "Untitled",
                Status = fc.Course.Status.ToString(),
            }).ToList(),
            Children = folder.Children.Select(c => new FolderChildDto
            {
                Id = c.Id,
                Name = c.Name,
            }).ToList(),
        });
    }

    public async Task<List<FolderTreeNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var folders = await _folderRepository.GetTreeAsync(ct);
        var lookup = folders.ToDictionary(f => f.Id);
        var roots = new List<FolderTreeNodeDto>();

        foreach (var folder in folders)
        {
            var node = new FolderTreeNodeDto
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentId = folder.ParentId,
            };

            if (folder.ParentId is null || !lookup.ContainsKey(folder.ParentId.Value))
            {
                roots.Add(node);
            }
        }

        // Build tree recursively
        return BuildTree(folders, null);
    }

    private List<FolderTreeNodeDto> BuildTree(List<Folder> folders, Guid? parentId)
    {
        return folders
            .Where(f => f.ParentId == parentId)
            .OrderBy(f => f.Name)
            .Select(f => new FolderTreeNodeDto
            {
                Id = f.Id,
                Name = f.Name,
                ParentId = f.ParentId,
                Children = BuildTree(folders, f.Id),
            })
            .ToList();
    }

    public async Task<ServiceResult<FolderDetailDto>> CreateFolderAsync(
        CreateFolderRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<FolderDetailDto>.Failure("Name is required.");

        if (request.ParentId.HasValue)
        {
            var parent = await _folderRepository.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null)
                return ServiceResult<FolderDetailDto>.Failure("Parent folder not found.");
        }

        var folder = new Folder
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentId = request.ParentId,
        };

        if (request.Attributes is { Count: > 0 })
        {
            foreach (var attr in request.Attributes)
            {
                folder.Attributes.Add(new FolderAttribute
                {
                    Key = attr.Key.Trim(),
                    Value = attr.Value.Trim(),
                });
            }
        }

        await _folderRepository.AddAsync(folder, ct);
        await _folderRepository.SaveChangesAsync(ct);

        return await GetFolderAsync(folder.Id, ct);
    }

    public async Task<ServiceResult<FolderDetailDto>> UpdateFolderAsync(
        Guid id, UpdateFolderRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<FolderDetailDto>.Failure("Name is required.");

        var folder = await _folderRepository.GetByIdWithDetailsAsync(id, ct);
        if (folder is null)
            return ServiceResult<FolderDetailDto>.Failure("Folder not found.");

        // Prevent circular reference: folder cannot be its own parent
        if (request.ParentId.HasValue && request.ParentId.Value == id)
            return ServiceResult<FolderDetailDto>.Failure("A folder cannot be its own parent.");

        if (request.ParentId.HasValue)
        {
            var parent = await _folderRepository.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null)
                return ServiceResult<FolderDetailDto>.Failure("Parent folder not found.");

            // Prevent circular: check that the new parent is not a descendant of this folder
            if (await IsDescendantAsync(request.ParentId.Value, id, ct))
                return ServiceResult<FolderDetailDto>.Failure("Cannot move a folder under its own descendant.");
        }

        folder.Name = request.Name.Trim();
        folder.Description = request.Description?.Trim();
        folder.HtmlContent = request.HtmlContent;
        folder.ParentId = request.ParentId;

        // Replace attributes
        folder.Attributes.Clear();
        if (request.Attributes is { Count: > 0 })
        {
            foreach (var attr in request.Attributes)
            {
                folder.Attributes.Add(new FolderAttribute
                {
                    Key = attr.Key.Trim(),
                    Value = attr.Value.Trim(),
                    FolderId = folder.Id,
                });
            }
        }

        await _folderRepository.SaveChangesAsync(ct);
        return await GetFolderAsync(folder.Id, ct);
    }

    public async Task<ServiceResult> AssignCourseAsync(
        Guid folderId, AssignCourseRequest request, CancellationToken ct = default)
    {
        var folder = await _folderRepository.GetByIdWithDetailsAsync(folderId, ct);
        if (folder is null)
            return ServiceResult.Failure("Folder not found.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId, ct);
        if (course is null)
            return ServiceResult.Failure("Course not found.");

        if (folder.FolderCourses.Any(fc => fc.CourseId == request.CourseId))
            return ServiceResult.Failure("Course is already assigned to this folder.");

        folder.FolderCourses.Add(new FolderCourse
        {
            FolderId = folderId,
            CourseId = request.CourseId,
        });

        await _folderRepository.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveCourseAsync(
        Guid folderId, Guid courseId, CancellationToken ct = default)
    {
        var folder = await _folderRepository.GetByIdWithDetailsAsync(folderId, ct);
        if (folder is null)
            return ServiceResult.Failure("Folder not found.");

        var fc = folder.FolderCourses.FirstOrDefault(x => x.CourseId == courseId);
        if (fc is null)
            return ServiceResult.Failure("Course is not assigned to this folder.");

        _folderRepository.Remove(fc);
        await _folderRepository.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private async Task<bool> IsDescendantAsync(Guid potentialDescendantId, Guid ancestorId, CancellationToken ct)
    {
        var current = await _folderRepository.GetByIdAsync(potentialDescendantId, ct);
        while (current is not null)
        {
            if (current.ParentId == ancestorId)
                return true;
            if (current.ParentId is null)
                break;
            current = await _folderRepository.GetByIdAsync(current.ParentId.Value, ct);
        }
        return false;
    }
}
