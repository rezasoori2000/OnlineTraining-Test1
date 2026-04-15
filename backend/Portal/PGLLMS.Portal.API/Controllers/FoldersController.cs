using Microsoft.AspNetCore.Mvc;
using PGLLMS.Portal.API.Services;

namespace PGLLMS.Portal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly PortalFolderService _service;

    public FoldersController(PortalFolderService service)
    {
        _service = service;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var tree = await _service.GetTreeAsync(ct);
        return Ok(tree);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFolder(Guid id, CancellationToken ct)
    {
        var folder = await _service.GetDetailAsync(id, ct);
        if (folder is null) return NotFound();
        return Ok(folder);
    }
}
