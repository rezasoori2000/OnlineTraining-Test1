namespace PGLLMS.Admin.Domain.Entities;

public class CourseTag
{
    public Guid CourseId { get; set; }
    public Guid TagId { get; set; }

    public Course Course { get; set; } = default!;
    public Tag Tag { get; set; } = default!;
}
