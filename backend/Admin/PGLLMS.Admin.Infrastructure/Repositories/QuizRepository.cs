using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.Infrastructure.Repositories;

public class QuizRepository : IQuizRepository
{
    private readonly AdminDbContext _context;

    public QuizRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Quiz quiz, CancellationToken ct = default)
        => await _context.Quizzes.AddAsync(quiz, ct);

    public async Task<Quiz?> GetByChapterIdAsync(Guid chapterId, CancellationToken ct = default)
        => await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .Include(q => q.Questions)
                .ThenInclude(q => q.Translations)
            .FirstOrDefaultAsync(q => q.ChapterId == chapterId, ct);

    public async Task DeleteByChapterIdAsync(Guid chapterId, CancellationToken ct = default)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.ChapterId == chapterId, ct);

        if (quiz is not null)
            _context.Quizzes.Remove(quiz);
    }
}
