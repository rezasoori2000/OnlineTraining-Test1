using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface ICourseRepository
{
    Task<List<Course>> GetAllAsync(CancellationToken ct = default);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Load course with translations + versions (chapters loaded separately).</summary>
    Task<Course?> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Course course, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
