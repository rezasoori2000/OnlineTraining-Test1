using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.Infrastructure.Repositories;

public class ChapterContentRepository : IChapterContentRepository
{
    private readonly AdminDbContext _context;

    public ChapterContentRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<ChapterContent?> GetByChapterIdAsync(Guid chapterId, CancellationToken ct = default)
        => await _context.ChapterContents
            .FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct);

    public Task UpdateAsync(ChapterContent content, string newHtml, CancellationToken ct = default)
    {
        content.HtmlContent = newHtml;
        return Task.CompletedTask; // EF tracks the entity; SaveChanges is called by the service
    }

    public async Task<HashSet<Guid>> GetChapterIdsWithContentAsync(
        IEnumerable<Guid> chapterIds, CancellationToken ct = default)
    {
        var ids = chapterIds.ToList();
        var result = await _context.ChapterContents
            .Where(cc => ids.Contains(cc.ChapterId))
            .Select(cc => cc.ChapterId)   // only fetch the FK — no HTML bytes
            .ToListAsync(ct);
        return result.ToHashSet();
    }

    public async Task AddAsync(ChapterContent content, CancellationToken ct = default)
        => await _context.ChapterContents.AddAsync(content, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
