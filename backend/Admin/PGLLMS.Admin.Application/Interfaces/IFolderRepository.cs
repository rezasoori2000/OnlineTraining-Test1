using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface IFolderRepository
{
    Task<List<Folder>> GetAllAsync(CancellationToken ct = default);
    Task<Folder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Folder?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<List<Folder>> GetTreeAsync(CancellationToken ct = default);
    Task AddAsync(Folder folder, CancellationToken ct = default);
    void Remove(FolderCourse folderCourse);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the ordered folder name segments from root to leaf for the folder that contains
    /// <paramref name="courseId"/>. E.g. ["F1", "F1-1"] for Root → F1 → F1-1.
    /// Returns an empty list if the course has no folder assignment.
    /// </summary>
    Task<List<string>> GetFolderPathForCourseAsync(Guid courseId, CancellationToken ct = default);
}
