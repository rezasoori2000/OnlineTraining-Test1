namespace PGLLMS.Admin.Infrastructure.Ai;

public class AiSettings
{
    public const string SectionName = "Ai";

    // ── Endpoints ─────────────────────────────────────────────────────────────
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string QdrantBaseUrl { get; set; } = "http://localhost:6333";

    /// <summary>Marker OCR sidecar base URL. In Docker: http://marker:8001, locally: http://localhost:8001.</summary>
    public string MarkerBaseUrl { get; set; } = "http://localhost:8001";

    // ── Models ────────────────────────────────────────────────────────────────
    /// <summary>Ollama model for embeddings. nomic-embed-text=768-dim; mxbai-embed-large=1024-dim.</summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>Ollama model for chat (llama3.2, mistral, phi3, gemma3, etc.).</summary>
    public string ChatModel { get; set; } = "llama3.2";

    // ── Qdrant collection ─────────────────────────────────────────────────────
    public string CollectionName { get; set; } = "pgllms_content";

    /// <summary>Must match the output dimension of EmbeddingModel exactly.</summary>
    public int VectorSize { get; set; } = 768;

    /// <summary>HNSW m parameter — number of bi-directional links per node. 16 is Qdrant default; 32 improves recall at cost of memory.</summary>
    public int HnswM { get; set; } = 16;

    /// <summary>HNSW ef_construct — larger = slower indexing, better recall. 200 is a good production default.</summary>
    public int HnswEfConstruct { get; set; } = 200;

    // ── Chunking ──────────────────────────────────────────────────────────────
    /// <summary>Target chunk size in characters. ~300 chars ≈ 80-100 tokens, well within nomic-embed-text 8192-token context.</summary>
    public int ChunkSizeChars { get; set; } = 800;

    /// <summary>Overlap between consecutive chunks as a fraction of ChunkSizeChars.</summary>
    public int ChunkOverlapChars { get; set; } = 120;

    // ── Retrieval ─────────────────────────────────────────────────────────────
    /// <summary>Number of nearest-neighbour candidates to retrieve from Qdrant.</summary>
    public int SearchLimit { get; set; } = 6;

    /// <summary>Minimum cosine similarity score (0-1) a chunk must have to be included in the context.
    /// 0.40 removes clearly irrelevant results while keeping borderline-relevant ones.</summary>
    public float ScoreThreshold { get; set; } = 0.40f;

    // ── LLM generation ───────────────────────────────────────────────────────
    /// <summary>Maximum context window sent to the LLM in tokens. Keep below model's ctx limit.</summary>
    public int LlmNumCtx { get; set; } = 4096;

    /// <summary>Maximum tokens the LLM will generate in its response.</summary>
    public int LlmNumPredict { get; set; } = 512;

    /// <summary>Temperature 0 = deterministic/factual. Increase slightly (0.1-0.3) for more natural phrasing.</summary>
    public float LlmTemperature { get; set; } = 0.1f;

    /// <summary>How many conversation turns (user+assistant pairs) to include as history.</summary>
    public int HistoryTurns { get; set; } = 3;
}
