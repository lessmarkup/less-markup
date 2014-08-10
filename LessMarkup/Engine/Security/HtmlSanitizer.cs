/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.Engine.Security
{
    public class HtmlSanitizer : IHtmlSanitizer
    {
        public static readonly HashSet<string> BlackList = new HashSet<string>
        {
            "script",
            "iframe",
            "form",
            "object",
            "embed",
            "link",
            "head",
            "meta"
        };

        public string Sanitize(string html, List<string> tagsToRemove = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var context = new SanitizeContext(tagsToRemove);

            context.Execute(doc.DocumentNode);

            using (var stringWriter = new StringWriter())
            {
                doc.DocumentNode.WriteTo(stringWriter);
                return stringWriter.ToString();
            }
        }

    }
}
