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
}
