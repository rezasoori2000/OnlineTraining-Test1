using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using PGLLMS.Admin.Application.DTOs.Chapter;
using PGLLMS.Admin.Application.Interfaces;
using PGLLMS.Admin.Domain.Entities;
using PGLLMS.Admin.Infrastructure.Persistence;

namespace PGLLMS.Admin.API.Services;

/// <summary>
/// Handles PDF upload for chapter content:
///   1. Extracts plain text from the PDF (for Qdrant indexing).
///   2. Determines the OneDrive folder path by mirroring the admin folder hierarchy.
///   3. Uploads the PDF to OneDrive.
///   4. Stores the OneDrive path + extracted text in the DB.
///   5. Indexes the text in Qdrant via IEmbeddingService.
/// </summary>
public class ChapterPdfService
{
    private readonly AdminDbContext _db;
    private readonly IChapterRepository _chapterRepo;
    private readonly IChapterContentRepository _contentRepo;
    private readonly ICourseVersionRepository _versionRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly IOneDriveService _oneDrive;
    private readonly IEmbeddingService _embedding;
    private readonly ILogger<ChapterPdfService> _logger;

    public ChapterPdfService(
        AdminDbContext db,
        IChapterRepository chapterRepo,
        IChapterContentRepository contentRepo,
        ICourseVersionRepository versionRepo,
        IFolderRepository folderRepo,
        IOneDriveService oneDrive,
        IEmbeddingService embedding,
        ILogger<ChapterPdfService> logger)
    {
        _db = db;
        _chapterRepo = chapterRepo;
        _contentRepo = contentRepo;
        _versionRepo = versionRepo;
        _folderRepo = folderRepo;
        _oneDrive = oneDrive;
        _embedding = embedding;
        _logger = logger;
    }

    public async Task<(bool Succeeded, string? Error, ChapterContentResponse? Data)> UploadPdfAsync(
        Guid chapterId,
        Stream pdfStream,
        string originalFileName,
        CancellationToken ct = default)
    {
        // ── 1. Load chapter + course version ─────────────────────────────────
        var chapter = await _chapterRepo.GetByIdAsync(chapterId, ct);
        if (chapter is null)
            return (false, "Chapter not found.", null);

        if (chapter.HasChildren)
            return (false, "Cannot add content to a chapter that has children.", null);

        var version = await _versionRepo.GetByIdAsync(chapter.CourseVersionId, ct);
        if (version is null)
            return (false, "Course version not found.", null);

        // ── 2. Load course translation (for chapter title) ────────────────────
        var chapterTitle = await _db.ChapterTranslations
            .Where(t => t.ChapterId == chapterId)
            .Select(t => t.Title)
            .FirstOrDefaultAsync(ct) ?? "untitled";

        // ── 3. Build OneDrive path ────────────────────────────────────────────
        var folderSegments = await _folderRepo.GetFolderPathForCourseAsync(version.CourseId, ct);
        var safeFileName = $"v{version.VersionNumber}-{SanitizeName(originalFileName)}";

        string remotePath;
        if (folderSegments.Count > 0)
        {
            var folderPath = string.Join("/", folderSegments.Select(SanitizeName));
            remotePath = $"{folderPath}/{safeFileName}";
        }
        else
        {
            remotePath = safeFileName;
        }

        // ── 4. Extract text from PDF ──────────────────────────────────────────
        string extractedText;
        byte[] pdfBytes;

        // Buffer the stream so we can read it twice (text extraction + upload)
        using (var ms = new MemoryStream())
        {
            await pdfStream.CopyToAsync(ms, ct);
            pdfBytes = ms.ToArray();
        }

        extractedText = ExtractTextFromPdf(pdfBytes);

        // ── 5. Upload to OneDrive ─────────────────────────────────────────────
        string uploadedPath;
        try
        {
            using var uploadStream = new MemoryStream(pdfBytes);
            uploadedPath = await _oneDrive.UploadFileAsync(remotePath, uploadStream, "application/pdf", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed for chapter {ChapterId}", chapterId);
            return (false, $"File upload failed: {ex.Message}", null);
        }

        // ── 6. Save to DB ─────────────────────────────────────────────────────
        var existingContent = await _contentRepo.GetByChapterIdAsync(chapterId, ct);
        ChapterContent content;

        if (existingContent is not null)
        {
            // Update existing record
            existingContent.HtmlContent = extractedText;
            existingContent.OneDriveFilePath = uploadedPath;
            content = existingContent;
        }
        else
        {
            content = new ChapterContent
            {
                ChapterId = chapterId,
                HtmlContent = extractedText,
                OneDriveFilePath = uploadedPath,
            };
            await _contentRepo.AddAsync(content, ct);
        }

        await _contentRepo.SaveChangesAsync(ct);

        // ── 7. Index in Qdrant (non-critical) ─────────────────────────────────
        if (!string.IsNullOrWhiteSpace(extractedText))
        {
            try
            {
                await _embedding.UpsertChapterAsync(
                    chapterId, version.CourseId, chapterTitle, extractedText, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Qdrant indexing failed for chapter {ChapterId}", chapterId);
                // Non-critical — upload to OneDrive succeeded; do not fail the response
            }
        }

        return (true, null, new ChapterContentResponse
        {
            Id = content.Id,
            ChapterId = chapterId,
            HtmlContent = extractedText,
            OneDriveFilePath = uploadedPath,
            CreatedAt = content.CreatedAt,
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ExtractTextFromPdf(byte[] bytes)
    {
        try
        {
            using var doc = PdfDocument.Open(bytes);
            var parts = new List<string>();
            foreach (var page in doc.GetPages())
            {
                var text = string.Join(" ", page.GetWords().Select(w => w.Text));
                if (!string.IsNullOrWhiteSpace(text))
                    parts.Add(text);
            }
            return string.Join("\n\n", parts);
        }
        catch (Exception)
        {
            return string.Empty; // Image-only or encrypted PDF — no text
        }
    }

    /// <summary>
    /// Strips characters that are invalid in OneDrive file/folder names.
    /// OneDrive prohibits: " * : < > ? / \ |  and leading/trailing spaces and dots.
    /// </summary>
    private static string SanitizeName(string name)
    {
        var invalid = new[] { '"', '*', ':', '<', '>', '?', '/', '\\', '|' };
        foreach (var c in invalid)
            name = name.Replace(c, '-');

        return name.Trim('.', ' ');
    }
}
