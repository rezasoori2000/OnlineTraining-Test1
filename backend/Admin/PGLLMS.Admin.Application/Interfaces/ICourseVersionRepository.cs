using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface ICourseVersionRepository
{
    Task<CourseVersion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CourseVersion?> GetPublishedVersionAsync(Guid courseId, CancellationToken ct = default);
    Task<int> GetNextVersionNumberAsync(Guid courseId, CancellationToken ct = default);
    Task AddAsync(CourseVersion version, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
