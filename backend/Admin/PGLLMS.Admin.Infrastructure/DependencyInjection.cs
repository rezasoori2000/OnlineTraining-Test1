using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PGLLMS.Admin.Domain.Identity;
using PGLLMS.Admin.Infrastructure.Ai;
using PGLLMS.Admin.Infrastructure.Persistence;
using PGLLMS.Admin.Infrastructure.Repositories;
using PGLLMS.Admin.Infrastructure.Security;
using PGLLMS.Admin.Infrastructure.Storage;
using AppInterfaces = PGLLMS.Admin.Application.Interfaces;

namespace PGLLMS.Admin.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AdminDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AdminDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
        .AddEntityFrameworkStores<AdminDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<AppInterfaces.ICourseRepository, CourseRepository>();
        services.AddScoped<AppInterfaces.ICourseVersionRepository, CourseVersionRepository>();
        services.AddScoped<AppInterfaces.IChapterRepository, ChapterRepository>();
        services.AddScoped<AppInterfaces.IChapterContentRepository, ChapterContentRepository>();
        services.AddScoped<AppInterfaces.IQuizRepository, QuizRepository>();
        services.AddScoped<AppInterfaces.IFolderRepository, FolderRepository>();

        services.AddSingleton<AppInterfaces.IHtmlSanitizer, GanssHtmlSanitizer>();

        // ── AI / RAG ──────────────────────────────────────────────────────────
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        services.AddHttpClient("Ollama", client =>
        {
            client.BaseAddress = new Uri(
                configuration["Ai:OllamaBaseUrl"] ?? "http://localhost:11434");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient("Qdrant", client =>
        {
            client.BaseAddress = new Uri(
                configuration["Ai:QdrantBaseUrl"] ?? "http://localhost:6333");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient("Marker", client =>
        {
            client.BaseAddress = new Uri(
                configuration["Ai:MarkerBaseUrl"] ?? "http://localhost:8001");
            // Large PDFs can take time to OCR — allow up to 10 minutes
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddScoped<AppInterfaces.IEmbeddingService, EmbeddingService>();

        // ── Local File Server (mapped drive storage) ──────────────────────────
        services.Configure<LocalFileServerSettings>(
            configuration.GetSection(LocalFileServerSettings.SectionName));

        services.AddSingleton<AppInterfaces.IOneDriveService, LocalFileServerService>();

        return services;
    }
}
