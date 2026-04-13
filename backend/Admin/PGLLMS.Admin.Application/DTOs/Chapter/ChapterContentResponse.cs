namespace PGLLMS.Admin.Application.DTOs.Chapter;

public class ChapterContentResponse
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public string HtmlContent { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
