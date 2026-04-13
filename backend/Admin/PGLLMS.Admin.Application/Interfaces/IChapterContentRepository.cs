using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface IChapterContentRepository
{
    Task<ChapterContent?> GetByChapterIdAsync(Guid chapterId, CancellationToken ct = default);
    Task AddAsync(ChapterContent content, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task UpdateAsync(ChapterContent content, string newHtml, CancellationToken ct = default);
    /// <summary>Returns the set of chapterIds (from the provided list) that have stored HTML content. Does NOT load the HTML itself.</summary>
    Task<HashSet<Guid>> GetChapterIdsWithContentAsync(IEnumerable<Guid> chapterIds, CancellationToken ct = default);
}
