using Microsoft.AspNetCore.Mvc;
using PGLLMS.Portal.API.DTOs;
using PGLLMS.Portal.API.Services;

namespace PGLLMS.Portal.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RagChatService _chatService;

    public ChatController(RagChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Send a question and receive a RAG-powered answer with source citations.
    /// Optional conversation history can be passed to maintain context.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { message = "Question is required." });

        var response = await _chatService.ChatAsync(request, ct);
        return Ok(response);
    }
}
