using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Domain.Identity;

namespace PGLLMS.Admin.Infrastructure.Persistence;

public class AdminDbContext : IdentityDbContext<ApplicationUser>
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseVersion> CourseVersions => Set<CourseVersion>();
    public DbSet<CourseTranslation> CourseTranslations => Set<CourseTranslation>();
    public DbSet<CourseTag> CourseTags => Set<CourseTag>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<ChapterContent> ChapterContents => Set<ChapterContent>();
    public DbSet<ChapterTranslation> ChapterTranslations => Set<ChapterTranslation>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();
    public DbSet<QuestionAnswerOption> QuestionAnswerOptions => Set<QuestionAnswerOption>();
    public DbSet<QuestionTranslation> QuestionTranslations => Set<QuestionTranslation>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<UserChapterProgress> UserChapterProgresses => Set<UserChapterProgress>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<FolderAttribute> FolderAttributes => Set<FolderAttribute>();
    public DbSet<FolderCourse> FolderCourses => Set<FolderCourse>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                    entity.CreatedAt = now;

                if (entry.State is EntityState.Added or EntityState.Modified)
                    entity.UpdatedAt = now;
            }
        }
    }
}
