using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGLLMS.Admin.Application.DTOs.Course;
using PGLLMS.Admin.Application.Services;

namespace PGLLMS.Admin.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly CourseService _courseService;
    private readonly FullCourseCreationService _fullCreationService;
    private readonly CourseDetailService _courseDetailService;
    private readonly CourseUpdateService _courseUpdateService;

    public CoursesController(
        CourseService courseService,
        FullCourseCreationService fullCreationService,
        CourseDetailService courseDetailService,
        CourseUpdateService courseUpdateService)
    {
        _courseService = courseService;
        _fullCreationService = fullCreationService;
        _courseDetailService = courseDetailService;
        _courseUpdateService = courseUpdateService;
    }

    /// <summary>
    /// Returns all courses with their primary translation title and latest version number.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses(CancellationToken ct)
    {
        var courses = await _courseService.GetAllCoursesAsync(ct);
        return Ok(courses);
    }

    /// <summary>
    /// Returns the full course detail for the active (draft-preferred) version,
    /// including the recursive chapter tree, per-leaf HTML content and quizzes.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourse([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _courseDetailService.GetAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(new { message = result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>
    /// Applies a change-set to an existing course (title/description, updated/new/deleted chapters,
    /// content replacements, quiz upserts). Version-safe: if the active version is Published a
    /// new draft version is created automatically.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCourse(
        [FromRoute] Guid id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken ct)
    {
        var result = await _courseUpdateService.UpdateAsync(id, request, ct);
        if (!result.Succeeded)
        {
            if (result.ErrorMessage!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new course (Status = Draft).
    /// Also creates the initial CourseVersion (v1, unpublished) and the primary CourseTranslation.
    /// Auto-generates a unique slug from the title.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken ct)
    {
        var result = await _courseService.CreateCourseAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(CreateCourse), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Creates a complete course with chapters, content (HTML), and quizzes in one transaction.
    /// Chapters are saved recursively. Leaf chapters must have HtmlContent.
    /// Non-leaf chapters must NOT have HtmlContent or a quiz.
    /// </summary>
    [HttpPost("full-create")]
    [ProducesResponseType(typeof(FullCreateCourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FullCreateCourse(
        [FromBody] FullCreateCourseRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _fullCreationService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(
            nameof(FullCreateCourse),
            new { id = result.Data!.CourseId },
            result.Data);
    }
}
