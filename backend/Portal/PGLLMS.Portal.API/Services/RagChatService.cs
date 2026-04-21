using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Infrastructure.Ai;
using PGLLMS.Portal.API.DTOs;

namespace PGLLMS.Portal.API.Services;

public class RagChatService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiSettings _settings;
    private readonly ILogger<RagChatService> _logger;

    public RagChatService(
        IEmbeddingService embeddingService,
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> settings,
        ILogger<RagChatService> logger)
    {
        _embeddingService = embeddingService;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        // ── 1. Search vector store — apply score threshold ───────────────────
        var rawResults = await _embeddingService.SearchAsync(
            request.Question, limit: _settings.SearchLimit, ct);

        var results = rawResults
            .Where(r => r.Score >= _settings.ScoreThreshold)
            .ToList();

        _logger.LogDebug("Vector search: {Raw} hits, {Filtered} above threshold {T:F2}",
            rawResults.Count, results.Count, _settings.ScoreThreshold);

        // ── 2. Build context block within token budget ────────────────────────
        // Approximate: 4 chars ≈ 1 token; reserve 1/3 of context window for prompt/answer
        int contextBudgetChars = _settings.LlmNumCtx * 3;
        int usedChars = 0;
        var contextParts = new List<string>();

        foreach (var (r, i) in results.Select((r, i) => (r, i)))
        {
            var part = $"[Source {i + 1}: {r.Title} ({r.Type})]\n{r.ChunkText}";
            if (usedChars + part.Length > contextBudgetChars) break;
            contextParts.Add(part);
            usedChars += part.Length;
        }

        var contextBlock = contextParts.Count > 0
            ? string.Join("\n\n", contextParts)
            : "No relevant content was found in the knowledge base.";

        var sources = results
            .Take(contextParts.Count)
            .DistinctBy(r => r.SourceId)
            .Select(r => new ChatSource(r.SourceId, r.Title, r.Type))
            .ToList();

        // ── 3. Build Ollama messages ──────────────────────────────────────────
        var systemPrompt =
            "You are a helpful assistant for an online training platform. " +
            "Answer questions ONLY based on the context provided below. " +
            "If the context does not contain enough information to answer, say so clearly and suggest " +
            "the user consult the relevant source material. " +
            "Always mention which source (title) you found the information in. " +
            "Never invent information not present in the context.\n\n" +
            "CONTEXT:\n" + contextBlock;

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        if (request.History is { Count: > 0 })
        {
            int historyLimit = _settings.HistoryTurns * 2;
            foreach (var msg in request.History.TakeLast(historyLimit))
                messages.Add(new { role = msg.Role, content = msg.Content });
        }

        messages.Add(new { role = "user", content = request.Question });

        // ── 4. Call Ollama with tuned options ─────────────────────────────────
        string answer;
        try
        {
            var ollama = _httpClientFactory.CreateClient("Ollama");
            var body = new
            {
                model = _settings.ChatModel,
                stream = false,
                messages,
                options = new
                {
                    temperature = _settings.LlmTemperature,
                    num_ctx = _settings.LlmNumCtx,
                    num_predict = _settings.LlmNumPredict
                }
            };
            var response = await ollama.PostAsJsonAsync("/api/chat", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama chat returned {Status}", response.StatusCode);
                answer = "Sorry, I could not get a response from the AI model right now.";
            }
            else
            {
                var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                    cancellationToken: ct);
                answer = result?.Message?.Content ?? "The AI model returned an empty response.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama chat call failed");
            answer = "The AI service is currently unavailable. Please try again later.";
        }

        return new ChatResponse(answer, sources);
    }

    // ── Private response DTOs ─────────────────────────────────────────────────

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
