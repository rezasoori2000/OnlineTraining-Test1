using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGLLMS.Admin.Application.DTOs.Lesson;
using PGLLMS.Admin.Application.Services;

namespace PGLLMS.Admin.API.Controllers;

[ApiController]
[Route("api/admin/lessons")]
public class LessonsController : ControllerBase
{
    private readonly LessonService _lessonService;

    public LessonsController(LessonService lessonService)
    {
        _lessonService = lessonService;
    }

    /// <summary>
    /// Creates a new lesson (Draft by default).
    /// Auto-generates a unique slug from the title.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLesson(
        [FromBody] CreateLessonRequest request,
        CancellationToken ct)
    {
        var result = await _lessonService.CreateLessonAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(CreateLesson), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Creates a new version for an existing lesson.
    /// </summary>
    [HttpPost("{lessonId:guid}/versions")]
    [ProducesResponseType(typeof(LessonVersionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVersion(
        [FromRoute] Guid lessonId,
        CancellationToken ct)
    {
        var request = new CreateLessonVersionRequest { LessonId = lessonId };
        var result = await _lessonService.CreateLessonVersionAsync(request, ct);

        if (!result.Succeeded)
            return NotFound(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(CreateVersion), new { lessonId }, result.Data);
    }
}
