using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class ChapterContent : BaseEntity
{
    public Guid ChapterId { get; set; }
    public string HtmlContent { get; set; } = default!;
    /// <summary>OneDrive relative path for PDF files (e.g. /F1/F1-1/v1-file.pdf). Null for non-PDF content.</summary>
    public string? OneDriveFilePath { get; set; }
    public Chapter Chapter { get; set; } = default!;
}
