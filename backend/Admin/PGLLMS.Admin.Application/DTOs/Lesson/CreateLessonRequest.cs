using System.ComponentModel.DataAnnotations;
using PGLLMS.Admin.Domain.Enums;

namespace PGLLMS.Admin.Application.DTOs.Lesson;

public class CreateLessonRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = default!;

    public LessonStatus Status { get; set; } = LessonStatus.Draft;

    public string? LanguageCode { get; set; } = "en";

    [MaxLength(2000)]
    public string? Description { get; set; }
}
