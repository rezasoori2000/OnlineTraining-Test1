using Microsoft.AspNetCore.Mvc;
using PGLLMS.Admin.Application.DTOs.Folder;
using PGLLMS.Admin.Application.Services;

namespace PGLLMS.Admin.API.Controllers;

[ApiController]
[Route("api/folders")]
public class FoldersController : ControllerBase
{
    private readonly FolderService _folderService;

    public FoldersController(FolderService folderService)
    {
        _folderService = folderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<FolderListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFolders(CancellationToken ct)
    {
        var folders = await _folderService.GetAllFoldersAsync(ct);
        return Ok(folders);
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(List<FolderTreeNodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var tree = await _folderService.GetTreeAsync(ct);
        return Ok(tree);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FolderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFolder([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _folderService.GetFolderAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(new { message = result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpPost]
    [ProducesResponseType(typeof(FolderDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFolder(
        [FromBody] CreateFolderRequest request, CancellationToken ct)
    {
        var result = await _folderService.CreateFolderAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });
        return CreatedAtAction(nameof(GetFolder), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FolderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFolder(
        [FromRoute] Guid id,
        [FromBody] UpdateFolderRequest request,
        CancellationToken ct)
    {
        var result = await _folderService.UpdateFolderAsync(id, request, ct);
        if (!result.Succeeded)
        {
            if (result.ErrorMessage!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }
        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/courses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignCourse(
        [FromRoute] Guid id,
        [FromBody] AssignCourseRequest request,
        CancellationToken ct)
    {
        var result = await _folderService.AssignCourseAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}/courses/{courseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveCourse(
        [FromRoute] Guid id,
        [FromRoute] Guid courseId,
        CancellationToken ct)
    {
        var result = await _folderService.RemoveCourseAsync(id, courseId, ct);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { success = true });
    }
}
