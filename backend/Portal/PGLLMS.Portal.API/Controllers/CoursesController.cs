using Microsoft.AspNetCore.Mvc;
using PGLLMS.Portal.API.Services;

namespace PGLLMS.Portal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly PortalCourseService _service;

    public CoursesController(PortalCourseService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCourse(Guid id, CancellationToken ct)
    {
        var course = await _service.GetCourseAsync(id, ct);
        if (course is null) return NotFound();
        return Ok(course);
    }

    [HttpGet("chapters/{chapterId:guid}/content")]
    public async Task<IActionResult> GetChapterContent(Guid chapterId, CancellationToken ct)
    {
        var content = await _service.GetChapterContentAsync(chapterId, ct);
        if (content is null) return NotFound();
        return Ok(content);
    }
}
