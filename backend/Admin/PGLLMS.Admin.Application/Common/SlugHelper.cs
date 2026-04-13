using System.Text;
using System.Text.RegularExpressions;

namespace PGLLMS.Admin.Application.Common;

public static class SlugHelper
{
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        var slug = title.ToLowerInvariant().Trim();

        // Replace spaces and common separators with hyphens
        slug = Regex.Replace(slug, @"[\s\-_]+", "-");

        // Remove all non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);

        // Trim leading/trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }
}
