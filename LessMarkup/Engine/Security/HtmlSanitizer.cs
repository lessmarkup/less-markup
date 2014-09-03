/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using HtmlAgilityPack;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.Engine.Security
{
    public class HtmlSanitizer : IHtmlSanitizer
    {
        public string Sanitize(string html, List<string> tagsToRemove = null, Func<IXPathNavigable, bool?> validateFunc = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var context = new SanitizeContext(tagsToRemove, validateFunc);

            context.Execute(doc.DocumentNode);

            using (var stringWriter = new StringWriter())
            {
                doc.DocumentNode.WriteTo(stringWriter);
                return stringWriter.ToString();
            }
        }

        public string ExtractText(string html)
        {
            if (html.IndexOf('<') < 0 && html.IndexOf('&') < 0)
            {
                return html;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.InnerText;
        }
    }
}
