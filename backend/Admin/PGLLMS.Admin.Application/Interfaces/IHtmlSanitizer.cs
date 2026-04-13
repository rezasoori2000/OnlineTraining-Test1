namespace PGLLMS.Admin.Application.Interfaces;

public interface IHtmlSanitizer
{
    string Sanitize(string html);
}
