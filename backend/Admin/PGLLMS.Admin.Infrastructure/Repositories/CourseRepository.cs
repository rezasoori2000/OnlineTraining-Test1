using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AdminDbContext _context;

    public CourseRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetAllAsync(CancellationToken ct = default)
        => await _context.Courses
            .Include(c => c.Translations)
            .Include(c => c.Versions)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public async Task<Course?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => await _context.Courses
            .Include(c => c.Translations)
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _context.Courses
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => await _context.Courses
            .AnyAsync(c => c.Slug == slug, ct);

    public async Task AddAsync(Course course, CancellationToken ct = default)
        => await _context.Courses.AddAsync(course, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
