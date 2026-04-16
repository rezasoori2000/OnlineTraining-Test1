using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PGLLMS.Admin.Application.Interfaces;

namespace PGLLMS.Admin.Infrastructure.Ai;

/// <summary>
/// Implements <see cref="IEmbeddingService"/> using Ollama for embeddings and Qdrant as the vector store.
/// All operations are fire-and-safe: exceptions are caught and logged so they never break the calling operation.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiSettings _settings;
    private readonly ILogger<EmbeddingService> _logger;
    private volatile bool _collectionEnsured;

    private const int ChunkSize = 1500;
    private const int ChunkOverlap = 150;
    private const int MaxTextLength = 12_000;

    public EmbeddingService(
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> settings,
        ILogger<EmbeddingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    // ── Public interface ────────────────────────────────────────────────────

    public async Task UpsertChapterAsync(Guid chapterId, Guid courseId, string chapterTitle,
        string htmlContent, CancellationToken ct = default)
    {
        try
        {
            var text = StripHtml(htmlContent);
            if (string.IsNullOrWhiteSpace(text)) return;

            await EnsureCollectionAsync(ct);

            var chunks = ChunkText(text);
            var points = new List<object>();

            for (var i = 0; i < chunks.Count; i++)
            {
                var embedding = await GetEmbeddingAsync(chunks[i], ct);
                if (embedding is null) return;

                points.Add(new
                {
                    id = DeterministicGuid($"{chapterId}_{i}").ToString(),
                    vector = embedding,
                    payload = new
                    {
                        source_id = chapterId.ToString(),
                        course_id = courseId.ToString(),
                        type = "chapter",
                        title = chapterTitle,
                        chunk_text = chunks[i][..Math.Min(500, chunks[i].Length)],
                        chunk_index = i
                    }
                });
            }

            await UpsertPointsAsync(points, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to embed chapter {ChapterId}", chapterId);
        }
    }

    public async Task UpsertFolderAsync(Guid folderId, string folderName,
        string htmlContent, CancellationToken ct = default)
    {
        try
        {
            var text = StripHtml(htmlContent);
            if (string.IsNullOrWhiteSpace(text)) return;

            await EnsureCollectionAsync(ct);

            var chunks = ChunkText(text);
            var points = new List<object>();

            for (var i = 0; i < chunks.Count; i++)
            {
                var embedding = await GetEmbeddingAsync(chunks[i], ct);
                if (embedding is null) return;

                points.Add(new
                {
                    id = DeterministicGuid($"{folderId}_{i}").ToString(),
                    vector = embedding,
                    payload = new
                    {
                        source_id = folderId.ToString(),
                        type = "folder",
                        title = folderName,
                        chunk_text = chunks[i][..Math.Min(500, chunks[i].Length)],
                        chunk_index = i
                    }
                });
            }

            await UpsertPointsAsync(points, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to embed folder {FolderId}", folderId);
        }
    }

    public async Task DeleteByCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        try
        {
            await DeleteByFilterAsync("course_id", courseId.ToString(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete embeddings for course {CourseId}", courseId);
        }
    }

    public async Task DeleteByFolderAsync(Guid folderId, CancellationToken ct = default)
    {
        try
        {
            await DeleteByFilterAsync("source_id", folderId.ToString(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete embeddings for folder {FolderId}", folderId);
        }
    }

    public async Task<List<EmbeddingSearchResult>> SearchAsync(string text, int limit = 5,
        CancellationToken ct = default)
    {
        try
        {
            var embedding = await GetEmbeddingAsync(text, ct);
            if (embedding is null) return [];

            var qdrant = _httpClientFactory.CreateClient("Qdrant");
            var body = new { vector = embedding, limit, with_payload = true };

            var response = await qdrant.PostAsJsonAsync(
                $"/collections/{_settings.CollectionName}/points/search", body, ct);

            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(
                cancellationToken: ct);

            return json?.Result?.Select(r => new EmbeddingSearchResult(
                GetPayloadString(r.Payload, "source_id"),
                GetPayloadString(r.Payload, "type"),
                GetPayloadString(r.Payload, "title"),
                GetPayloadString(r.Payload, "chunk_text"),
                r.Score
            )).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector search failed for query");
            return [];
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private async Task EnsureCollectionAsync(CancellationToken ct)
    {
        if (_collectionEnsured) return;

        var qdrant = _httpClientFactory.CreateClient("Qdrant");
        var body = new { vectors = new { size = _settings.VectorSize, distance = "Cosine" } };

        // 200 = created; 400 = already exists — both are acceptable
        await qdrant.PutAsJsonAsync($"/collections/{_settings.CollectionName}", body, ct);
        _collectionEnsured = true;
    }

    private async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct)
    {
        var ollama = _httpClientFactory.CreateClient("Ollama");
        var body = new { model = _settings.EmbeddingModel, prompt = text };
        var response = await ollama.PostAsJsonAsync("/api/embeddings", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama embedding returned {Status}", response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
            cancellationToken: ct);
        return result?.Embedding;
    }

    private async Task UpsertPointsAsync(List<object> points, CancellationToken ct)
    {
        if (points.Count == 0) return;
        var qdrant = _httpClientFactory.CreateClient("Qdrant");
        await qdrant.PutAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points", new { points }, ct);
    }

    private async Task DeleteByFilterAsync(string key, string value, CancellationToken ct)
    {
        var qdrant = _httpClientFactory.CreateClient("Qdrant");
        var body = new
        {
            filter = new
            {
                must = new[] { new { key, match = new { value } } }
            }
        };
        await qdrant.PostAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points/delete", body, ct);
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim()[..Math.Min(text.Trim().Length, MaxTextLength)];
    }

    private static List<string> ChunkText(string text)
    {
        if (text.Length <= ChunkSize) return [text];

        var chunks = new List<string>();
        var i = 0;
        while (i < text.Length)
        {
            var end = Math.Min(i + ChunkSize, text.Length);
            chunks.Add(text[i..end]);
            i += ChunkSize - ChunkOverlap;
        }
        return chunks;
    }

    private static Guid DeterministicGuid(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    private static string GetPayloadString(Dictionary<string, JsonElement> payload, string key)
        => payload.TryGetValue(key, out var el) ? el.GetString() ?? "" : "";

    // ── Response DTOs ───────────────────────────────────────────────────────

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    private sealed class QdrantSearchResponse
    {
        [JsonPropertyName("result")]
        public List<QdrantScoredPoint>? Result { get; set; }
    }

    private sealed class QdrantScoredPoint
    {
        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, JsonElement> Payload { get; set; } = new();
    }
}
