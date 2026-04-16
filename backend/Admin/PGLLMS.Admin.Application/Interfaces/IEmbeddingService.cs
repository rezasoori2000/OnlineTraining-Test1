namespace PGLLMS.Admin.Application.Interfaces;

public interface IEmbeddingService
{
    /// <summary>Embed chapter content and store in the vector database.</summary>
    Task UpsertChapterAsync(Guid chapterId, Guid courseId, string chapterTitle, string htmlContent, CancellationToken ct = default);

    /// <summary>Embed folder HTML content and store in the vector database.</summary>
    Task UpsertFolderAsync(Guid folderId, string folderName, string htmlContent, CancellationToken ct = default);

    /// <summary>Remove all vectors belonging to a course (called on archive).</summary>
    Task DeleteByCourseAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>Remove all vectors belonging to a folder.</summary>
    Task DeleteByFolderAsync(Guid folderId, CancellationToken ct = default);

    /// <summary>Embed a query and return the top-k semantically similar chunks.</summary>
    Task<List<EmbeddingSearchResult>> SearchAsync(string text, int limit = 5, CancellationToken ct = default);
}

public record EmbeddingSearchResult(string SourceId, string Type, string Title, string ChunkText, float Score);
