using System.ComponentModel.DataAnnotations;

namespace PGLLMS.Admin.Application.DTOs.Chapter;

public class UploadChapterContentRequest
{
    [Required]
    public Guid ChapterId { get; set; }

    [Required]
    public string HtmlContent { get; set; } = default!;
}
