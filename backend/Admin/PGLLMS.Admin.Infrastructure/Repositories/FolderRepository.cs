using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.Infrastructure.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly AdminDbContext _context;

    public FolderRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<List<Folder>> GetAllAsync(CancellationToken ct = default)
        => await _context.Folders
            .Include(f => f.Parent)
            .Include(f => f.Children)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

    public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Folders
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Folder?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _context.Folders
            .Include(f => f.Parent)
            .Include(f => f.Children)
            .Include(f => f.Attributes)
            .Include(f => f.FolderCourses)
                .ThenInclude(fc => fc.Course)
                    .ThenInclude(c => c.Translations)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<List<Folder>> GetTreeAsync(CancellationToken ct = default)
        => await _context.Folders
            .Include(f => f.Children)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

    public async Task AddAsync(Folder folder, CancellationToken ct = default)
        => await _context.Folders.AddAsync(folder, ct);

    public void Remove(FolderCourse folderCourse)
        => _context.FolderCourses.Remove(folderCourse);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task<List<string>> GetFolderPathForCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        // Find the folder that directly contains the course
        var folderCourse = await _context.FolderCourses
            .FirstOrDefaultAsync(fc => fc.CourseId == courseId, ct);

        if (folderCourse is null) return new List<string>();

        // Load all folders into a flat dictionary and walk up the tree
        var allFolders = await _context.Folders.ToListAsync(ct);
        var dict = allFolders.ToDictionary(f => f.Id);

        var path = new List<string>();
        Guid? current = folderCourse.FolderId;

        while (current.HasValue && dict.TryGetValue(current.Value, out var folder))
        {
            path.Insert(0, folder.Name);
            current = folder.ParentId;
        }

        return path;
    }
}
