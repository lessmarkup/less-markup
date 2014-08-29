using System.Collections.Generic;

namespace LessMarkup.Interfaces.Security
{
    public interface IHtmlSanitizer
    {
        string Sanitize(string html, List<string> tagsToRemove = null);
        string ExtractText(string html);
    }
}
