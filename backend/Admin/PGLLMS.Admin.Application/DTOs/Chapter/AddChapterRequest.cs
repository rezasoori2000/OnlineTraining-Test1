using System.ComponentModel.DataAnnotations;

namespace PGLLMS.Admin.Application.DTOs.Chapter;

public class AddChapterRequest
{
    [Required]
    public Guid CourseVersionId { get; set; }

    public Guid? ParentId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = default!;

    public string LanguageCode { get; set; } = "en";
}
