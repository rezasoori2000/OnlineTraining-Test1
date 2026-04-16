using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.Infrastructure.Repositories;

public class ChapterRepository : IChapterRepository
{
    private readonly AdminDbContext _context;

    public ChapterRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<Chapter?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Chapter?> GetByIdWithTranslationsAsync(Guid id, CancellationToken ct = default)
        => await _context.Chapters
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Chapter>> GetFullByVersionAsync(Guid versionId, CancellationToken ct = default)
        => await _context.Chapters
            .Where(c => c.CourseVersionId == versionId)
            .Include(c => c.Translations)
            // c.Content (HtmlContent) intentionally excluded — load on demand via GetChapterIdsWithContentAsync
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.Options)
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.Translations)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

    public async Task SoftDeleteWithDescendantsAsync(Guid chapterId, CancellationToken ct = default)
    {
        // Load the chapter and all descendants then mark IsDeleted
        var all = await _context.Chapters
            .Where(c => c.CourseVersionId ==
                _context.Chapters.Where(x => x.Id == chapterId).Select(x => x.CourseVersionId).FirstOrDefault())
            .ToListAsync(ct);

        var toDelete = new List<Chapter>();
        CollectSubtree(all, chapterId, toDelete);

        foreach (var ch in toDelete)
            ch.IsDeleted = true;
    }

    private static void CollectSubtree(List<Chapter> all, Guid rootId, List<Chapter> result)
    {
        var node = all.FirstOrDefault(c => c.Id == rootId);
        if (node is null) return;
        result.Add(node);
        foreach (var child in all.Where(c => c.ParentId == rootId))
            CollectSubtree(all, child.Id, result);
    }

    public async Task<int> GetNextOrderAsync(Guid courseVersionId, Guid? parentId, CancellationToken ct = default)
    {
        var max = await _context.Chapters
            .Where(c => c.CourseVersionId == courseVersionId && c.ParentId == parentId)
            .MaxAsync(c => (int?)c.Order, ct);

        return (max ?? 0) + 1;
    }

    public async Task AddAsync(Chapter chapter, CancellationToken ct = default)
        => await _context.Chapters.AddAsync(chapter, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
