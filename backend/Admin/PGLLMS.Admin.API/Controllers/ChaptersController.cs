using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGLLMS.Admin.Application.DTOs.Chapter;
using PGLLMS.Admin.Application.Services;
using PGLLMS.Admin.API.Services;

namespace PGLLMS.Admin.API.Controllers;

[ApiController]
[Route("api/admin/chapters")]
public class ChaptersController : ControllerBase
{
    private readonly ChapterService _chapterService;
    private readonly ChapterPdfService _pdfService;

    public ChaptersController(ChapterService chapterService, ChapterPdfService pdfService)
    {
        _chapterService = chapterService;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Adds a chapter to a lesson version.
    /// If ParentId is provided the chapter is created as a child.
    /// Order is auto-assigned.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChapterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddChapter(
        [FromBody] AddChapterRequest request,
        CancellationToken ct)
    {
        var result = await _chapterService.AddChapterAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(nameof(AddChapter), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Uploads sanitized HTML content to a leaf chapter.
    /// Returns 400 if the chapter has children (hierarchical rule enforcement).
    /// </summary>
    [HttpPost("{chapterId:guid}/content")]
    [ProducesResponseType(typeof(ChapterContentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadContent(
        [FromRoute] Guid chapterId,
        [FromBody] UploadChapterContentRequest request,
        CancellationToken ct)
    {
        request.ChapterId = chapterId;
        var result = await _chapterService.UploadContentAsync(request, ct);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = result.ErrorMessage });

            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(UploadContent), new { chapterId }, result.Data);
    }

    /// <summary>
    /// Uploads a PDF file for a leaf chapter. The file is stored in OneDrive and its
    /// text is extracted and indexed in Qdrant. The OneDrive path is saved in the DB.
    /// </summary>
    [HttpPost("{chapterId:guid}/pdf")]
    [ProducesResponseType(typeof(ChapterContentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPdf(
        [FromRoute] Guid chapterId,
        IFormFile pdf,
        CancellationToken ct)
    {
        if (pdf is null || pdf.Length == 0)
            return BadRequest(new { message = "No PDF file provided." });

        var ext = Path.GetExtension(pdf.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest(new { message = "Only PDF files are accepted." });

        using var stream = pdf.OpenReadStream();
        var (succeeded, error, data) = await _pdfService.UploadPdfAsync(chapterId, stream, pdf.FileName, ct);

        if (!succeeded)
        {
            if (error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(UploadPdf), new { chapterId }, data);
    }
}
