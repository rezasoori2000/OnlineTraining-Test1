using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface ILessonVersionRepository
{
    Task<LessonVersion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LessonVersion?> GetPublishedVersionAsync(Guid lessonId, CancellationToken ct = default);
    Task<int> GetNextVersionNumberAsync(Guid lessonId, CancellationToken ct = default);
    Task AddAsync(LessonVersion version, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
