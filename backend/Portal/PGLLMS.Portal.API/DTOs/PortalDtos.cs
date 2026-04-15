namespace PGLLMS.Portal.API.DTOs;

// ── Folder DTOs ──

public record PortalFolderTreeNodeDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    List<PortalFolderTreeNodeDto> Children);

public record PortalFolderDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? HtmlContent,
    List<PortalFolderCourseDto> Courses);

public record PortalFolderCourseDto(
    Guid CourseId,
    string Title,
    string? Description);

// ── Course DTOs ──

public record PortalCourseDetailDto(
    Guid Id,
    string Title,
    string? Description,
    List<PortalChapterNodeDto> Chapters);

public record PortalChapterNodeDto(
    Guid Id,
    string Title,
    int Order,
    bool HasContent,
    List<PortalChapterNodeDto> Children);

public record PortalChapterContentDto(
    Guid ChapterId,
    string Title,
    string? HtmlContent);
