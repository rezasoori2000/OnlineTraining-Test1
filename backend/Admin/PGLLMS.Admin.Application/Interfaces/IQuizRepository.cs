using PGLLMS.Admin.Domain.Entities;

namespace PGLLMS.Admin.Application.Interfaces;

public interface IQuizRepository
{
    Task AddAsync(Quiz quiz, CancellationToken ct = default);
    Task<Quiz?> GetByChapterIdAsync(Guid chapterId, CancellationToken ct = default);
    Task DeleteByChapterIdAsync(Guid chapterId, CancellationToken ct = default);
}
