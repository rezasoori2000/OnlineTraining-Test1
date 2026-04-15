namespace PGLLMS.Admin.Application.DTOs.Folder;

public class FolderListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ParentName { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FolderDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? HtmlContent { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public List<FolderAttributeDto> Attributes { get; set; } = new();
    public List<FolderCourseDto> Courses { get; set; } = new();
    public List<FolderChildDto> Children { get; set; } = new();
}

public class FolderAttributeDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}

public class FolderCourseDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = default!;
    public string Status { get; set; } = default!;
}

public class FolderChildDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class FolderTreeNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public List<FolderTreeNodeDto> Children { get; set; } = new();
}

public class CreateFolderRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public List<FolderAttributeRequest>? Attributes { get; set; }
}

public class UpdateFolderRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? HtmlContent { get; set; }
    public Guid? ParentId { get; set; }
    public List<FolderAttributeRequest>? Attributes { get; set; }
}

public class FolderAttributeRequest
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}

public class AssignCourseRequest
{
    public Guid CourseId { get; set; }
}
