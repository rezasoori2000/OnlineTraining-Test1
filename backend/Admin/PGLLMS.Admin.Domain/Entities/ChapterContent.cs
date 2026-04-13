using PGLLMS.Admin.Domain.Common;

namespace PGLLMS.Admin.Domain.Entities;

public class ChapterContent : BaseEntity
{
    public Guid ChapterId { get; set; }
    public string HtmlContent { get; set; } = default!;

    public Chapter Chapter { get; set; } = default!;
}
