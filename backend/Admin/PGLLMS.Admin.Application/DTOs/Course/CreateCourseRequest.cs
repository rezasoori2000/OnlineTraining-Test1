using System.ComponentModel.DataAnnotations;

namespace PGLLMS.Admin.Application.DTOs.Course;

public class CreateCourseRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = default!;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = default!;

    public string LanguageCode { get; set; } = "en";
}
