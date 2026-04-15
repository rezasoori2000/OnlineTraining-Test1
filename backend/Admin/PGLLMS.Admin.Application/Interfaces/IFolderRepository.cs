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
}
