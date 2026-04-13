namespace PGLLMS.Admin.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;

    public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
}
