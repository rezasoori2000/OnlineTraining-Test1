using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface IChapterRepository
{
    Task<Chapter?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Load a single chapter including its Translations collection (needed for updates).</summary>
    Task<Chapter?> GetByIdWithTranslationsAsync(Guid id, CancellationToken ct = default);
    Task<int> GetNextOrderAsync(Guid courseVersionId, Guid? parentId, CancellationToken ct = default);
    Task AddAsync(Chapter chapter, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Load all chapters for a version with translations, content and quizzes (questions + options + translations).</summary>
    Task<List<Chapter>> GetFullByVersionAsync(Guid versionId, CancellationToken ct = default);
    /// <summary>Soft-delete a chapter and all its descendants.</summary>
    Task SoftDeleteWithDescendantsAsync(Guid chapterId, CancellationToken ct = default);
}
