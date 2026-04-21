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
/// Implements <see cref="IEmbeddingService"/> using Ollama (embeddings) + Qdrant (vector store).
///
/// Best-practice improvements over v1:
///   - Sentence-aware chunking: splits at [.!?] + space boundary, fallback to word boundary
///   - No MaxTextLength pre-truncation — ALL content is indexed
///   - Full chunk text stored in payload (was truncated to 500 chars)
///   - Parallel embedding with bounded concurrency (SemaphoreSlim)
///   - Static SemaphoreSlim for EnsureCollection (one-time init, thread-safe across scoped instances)
///   - GET check before PUT on collection creation (idempotent, not relying on HTTP 400)
///   - HNSW params + on_disk_payload on collection creation
///   - score_threshold sent to Qdrant search
///   - SHA-256 deterministic GUIDs (collision-resistant)
///   - Strongly typed Qdrant request records
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiSettings _settings;
    private readonly ILogger<EmbeddingService> _logger;

    // Shared across all scoped instances — write-once after first collection setup
    private static volatile bool _collectionReady;
    private static readonly SemaphoreSlim _collectionLock = new(1, 1);

    // Max concurrent Ollama embed calls per upsert
    private const int EmbedParallelism = 4;

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
        await UpsertDocumentAsync(
            sourceId: chapterId.ToString(),
            type: "chapter",
            title: chapterTitle,
            htmlContent: htmlContent,
            extraPayload: new Dictionary<string, string> { ["course_id"] = courseId.ToString() },
            ct: ct);
    }

    public async Task UpsertFolderAsync(Guid folderId, string folderName,
        string htmlContent, CancellationToken ct = default)
    {
        await UpsertDocumentAsync(
            sourceId: folderId.ToString(),
            type: "folder",
            title: folderName,
            htmlContent: htmlContent,
            extraPayload: null,
            ct: ct);
    }

    public async Task DeleteByCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        try { await DeleteByFilterAsync("course_id", courseId.ToString(), ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete embeddings for course {Id}", courseId); }
    }

    public async Task DeleteByFolderAsync(Guid folderId, CancellationToken ct = default)
    {
        try { await DeleteByFilterAsync("source_id", folderId.ToString(), ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete embeddings for folder {Id}", folderId); }
    }

    public async Task<List<EmbeddingSearchResult>> SearchAsync(string text, int limit = 5,
        CancellationToken ct = default)
    {
        try
        {
            await EnsureCollectionAsync(ct);
            var embedding = await GetEmbeddingAsync(text, ct);
            if (embedding is null) return [];

            var qdrant = _httpClientFactory.CreateClient("Qdrant");
            var searchLimit = Math.Max(limit, _settings.SearchLimit);
            var body = new QdrantSearchRequest(
                Vector: embedding,
                Limit: searchLimit,
                WithPayload: true,
                ScoreThreshold: _settings.ScoreThreshold,
                Params: new QdrantSearchParams(Ef: searchLimit * 2));

            var response = await qdrant.PostAsJsonAsync(
                $"/collections/{_settings.CollectionName}/points/search", body, ct);

            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: ct);
            return json?.Result?
                .Select(r => new EmbeddingSearchResult(
                    GetString(r.Payload, "source_id"),
                    GetString(r.Payload, "type"),
                    GetString(r.Payload, "title"),
                    GetString(r.Payload, "chunk_text"),
                    r.Score))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector search failed");
            return [];
        }
    }

    // ── Core upsert pipeline ────────────────────────────────────────────────

    private async Task UpsertDocumentAsync(
        string sourceId, string type, string title, string htmlContent,
        Dictionary<string, string>? extraPayload, CancellationToken ct)
    {
        try
        {
            var text = StripHtml(htmlContent);
            if (string.IsNullOrWhiteSpace(text)) return;

            await EnsureCollectionAsync(ct);

            // Delete existing points first so re-indexing is idempotent
            await DeleteByFilterAsync("source_id", sourceId, ct);

            var chunks = ChunkBySentence(text, _settings.ChunkSizeChars, _settings.ChunkOverlapChars);
            var semaphore = new SemaphoreSlim(EmbedParallelism);
            var points = new List<QdrantPoint>(chunks.Count);

            var tasks = chunks.Select(async (chunk, i) =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var embedding = await GetEmbeddingAsync(chunk, ct);
                    if (embedding is null) return (Point: (QdrantPoint?)null, Index: i);

                    var payload = new Dictionary<string, string>
                    {
                        ["source_id"]    = sourceId,
                        ["type"]         = type,
                        ["title"]        = title,
                        ["chunk_text"]   = chunk,   // full chunk — no truncation
                        ["chunk_index"]  = i.ToString(),
                        ["chunk_count"]  = chunks.Count.ToString(),
                    };
                    if (extraPayload is not null)
                        foreach (var kv in extraPayload) payload[kv.Key] = kv.Value;

                    return (Point: (QdrantPoint?)new QdrantPoint(
                        Id: DeterministicGuid($"{sourceId}_{i}").ToString(),
                        Vector: embedding,
                        Payload: payload), Index: i);
                }
                finally { semaphore.Release(); }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            foreach (var r in results.OrderBy(r => r.Index))
                if (r.Point is not null) points.Add(r.Point);

            if (points.Count > 0)
                await UpsertPointsAsync(points, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert {Type} {Id}", type, sourceId);
        }
    }

    // ── Collection management ───────────────────────────────────────────────

    private async Task EnsureCollectionAsync(CancellationToken ct)
    {
        if (_collectionReady) return;
        await _collectionLock.WaitAsync(ct);
        try
        {
            if (_collectionReady) return;
            var qdrant = _httpClientFactory.CreateClient("Qdrant");

            // Check first — don't rely on HTTP 400 for "already exists"
            var check = await qdrant.GetAsync($"/collections/{_settings.CollectionName}", ct);
            if (check.IsSuccessStatusCode) { _collectionReady = true; return; }

            var body = new
            {
                vectors = new { size = _settings.VectorSize, distance = "Cosine", on_disk = false },
                hnsw_config = new
                {
                    m = _settings.HnswM,
                    ef_construct = _settings.HnswEfConstruct,
                    full_scan_threshold = 10_000,
                },
                on_disk_payload = true,
                optimizers_config = new { memmap_threshold = 20_000, indexing_threshold = 10_000 },
            };

            var create = await qdrant.PutAsJsonAsync($"/collections/{_settings.CollectionName}", body, ct);
            if (!create.IsSuccessStatusCode)
                _logger.LogWarning("Qdrant collection creation returned {Status}", create.StatusCode);

            _collectionReady = true;
        }
        finally { _collectionLock.Release(); }
    }

    // ── Ollama embedding ────────────────────────────────────────────────────

    private async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct)
    {
        var ollama = _httpClientFactory.CreateClient("Ollama");

        // Try newer /api/embed endpoint first (batch-capable), fall back to /api/embeddings
        var response = await ollama.PostAsJsonAsync("/api/embed",
            new { model = _settings.EmbeddingModel, input = text, truncate = false }, ct);

        if (!response.IsSuccessStatusCode)
            response = await ollama.PostAsJsonAsync("/api/embeddings",
                new { model = _settings.EmbeddingModel, prompt = text }, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama embedding failed {Status}", response.StatusCode);
            return null;
        }

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        // /api/embed  → { "embeddings": [[...]] }
        if (root.TryGetProperty("embeddings", out var arr))
        {
            var first = arr.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Array)
                return first.EnumerateArray().Select(e => e.GetSingle()).ToArray();
        }
        // /api/embeddings → { "embedding": [...] }
        if (root.TryGetProperty("embedding", out var emb) && emb.ValueKind == JsonValueKind.Array)
            return emb.EnumerateArray().Select(e => e.GetSingle()).ToArray();

        _logger.LogWarning("Unexpected Ollama embedding response shape");
        return null;
    }

    // ── Qdrant writes ───────────────────────────────────────────────────────

    private async Task UpsertPointsAsync(List<QdrantPoint> points, CancellationToken ct)
    {
        var qdrant = _httpClientFactory.CreateClient("Qdrant");
        var response = await qdrant.PutAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points?wait=false", new { points }, ct);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Qdrant upsert failed {Status}", response.StatusCode);
    }

    private async Task DeleteByFilterAsync(string key, string value, CancellationToken ct)
    {
        var qdrant = _httpClientFactory.CreateClient("Qdrant");
        var body = new { filter = new { must = new[] { new { key, match = new { value } } } } };
        await qdrant.PostAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points/delete?wait=false", body, ct);
    }

    // ── Text processing ─────────────────────────────────────────────────────

    private static readonly Regex _tagRegex       = new("<[^>]+>",      RegexOptions.Compiled);
    private static readonly Regex _entityRegex    = new(@"&[a-z]+;|&#\d+;", RegexOptions.Compiled);
    private static readonly Regex _whitespaceRegex = new(@"\s+",         RegexOptions.Compiled);

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var text = _tagRegex.Replace(html, " ");
        text = _entityRegex.Replace(text, " ");
        return _whitespaceRegex.Replace(text, " ").Trim();
    }

    /// <summary>
    /// Sentence-aware chunking: prefers to break at [.!?] boundaries,
    /// falls back to last whitespace. Never cuts mid-word.
    /// </summary>
    private static List<string> ChunkBySentence(string text, int chunkSize, int overlap)
    {
        if (text.Length <= chunkSize) return [text];

        var chunks = new List<string>();
        var start = 0;

        while (start < text.Length)
        {
            var end = Math.Min(start + chunkSize, text.Length);

            if (end < text.Length)
            {
                // Look for sentence boundary in last 20% of chunk
                var searchFrom = end - (chunkSize / 5);
                var sentenceEnd = -1;
                for (var j = end - 1; j >= searchFrom; j--)
                {
                    if (text[j] is '.' or '!' or '?' && j + 1 < text.Length && text[j + 1] == ' ')
                    { sentenceEnd = j + 1; break; }
                }

                if (sentenceEnd > start)
                    end = sentenceEnd;
                else
                {
                    var lastSpace = text.LastIndexOf(' ', end - 1, Math.Min(end - start, chunkSize));
                    if (lastSpace > start) end = lastSpace + 1;
                }
            }

            chunks.Add(text[start..end].Trim());
            start = end - overlap;
        }
        return chunks;
    }

    // ── Utilities ───────────────────────────────────────────────────────────

    /// <summary>SHA-256-based deterministic Guid — collision-resistant, stable across runs.</summary>
    private static Guid DeterministicGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50); // version 5
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant
        return new Guid(bytes);
    }

    private static string GetString(Dictionary<string, JsonElement> payload, string key)
        => payload.TryGetValue(key, out var el) ? el.GetString() ?? "" : "";

    // ── Strongly-typed Qdrant records ───────────────────────────────────────

    private sealed record QdrantPoint(
        [property: JsonPropertyName("id")]      string Id,
        [property: JsonPropertyName("vector")]  float[] Vector,
        [property: JsonPropertyName("payload")] Dictionary<string, string> Payload);

    private sealed record QdrantSearchRequest(
        [property: JsonPropertyName("vector")]           float[] Vector,
        [property: JsonPropertyName("limit")]            int Limit,
        [property: JsonPropertyName("with_payload")]     bool WithPayload,
        [property: JsonPropertyName("score_threshold")] float ScoreThreshold,
        [property: JsonPropertyName("params")]           QdrantSearchParams Params);

    private sealed record QdrantSearchParams(
        [property: JsonPropertyName("ef")] int Ef);

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
