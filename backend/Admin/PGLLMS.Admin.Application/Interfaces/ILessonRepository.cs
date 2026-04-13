using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Lesson?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
