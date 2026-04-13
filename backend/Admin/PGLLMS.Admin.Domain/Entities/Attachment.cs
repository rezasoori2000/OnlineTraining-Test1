namespace PGLLMS.Admin.Domain.Entities;

public class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;

    public Course Course { get; set; } = default!;
}
