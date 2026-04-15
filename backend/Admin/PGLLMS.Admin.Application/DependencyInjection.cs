using Microsoft.Extensions.DependencyInjection;
using PGLLMS.Admin.Application.Services;

namespace PGLLMS.Admin.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CourseService>();
        services.AddScoped<CourseDetailService>();
        services.AddScoped<CourseUpdateService>();
        services.AddScoped<ChapterService>();
        services.AddScoped<FullCourseCreationService>();
        services.AddScoped<FolderService>();
        return services;
    }
}
