using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace LessMarkup.Interfaces.Security
{
    public interface IHtmlSanitizer
    {
        string Sanitize(string html, List<string> tagsToRemove = null, Func<IXPathNavigable, bool?> validateFunc = null);
        string ExtractText(string html);
    }
}
