using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.Infrastructure.Repositories;

public class CourseVersionRepository : ICourseVersionRepository
{
    private readonly AdminDbContext _context;

    public CourseVersionRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<CourseVersion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CourseVersions
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<CourseVersion?> GetPublishedVersionAsync(Guid courseId, CancellationToken ct = default)
        => await _context.CourseVersions
            .FirstOrDefaultAsync(v => v.CourseId == courseId && v.IsPublished, ct);

    public async Task<int> GetNextVersionNumberAsync(Guid courseId, CancellationToken ct = default)
    {
        var max = await _context.CourseVersions
            .Where(v => v.CourseId == courseId)
            .MaxAsync(v => (int?)v.VersionNumber, ct);

        return (max ?? 0) + 1;
    }

    public async Task AddAsync(CourseVersion version, CancellationToken ct = default)
        => await _context.CourseVersions.AddAsync(version, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
