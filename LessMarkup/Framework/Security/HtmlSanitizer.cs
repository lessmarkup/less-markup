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

        public string Sanitize(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            SanitizeHtmlNode(doc.DocumentNode);

            using (var stringWriter = new StringWriter())
            {
                doc.DocumentNode.WriteTo(stringWriter);
                return stringWriter.ToString();
            }
        }

        private static void SanitizeHtmlNode(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                // check for blacklist items and remove
                if (BlackList.Contains(node.Name))
                {
                    node.Remove();
                    return;
                }

                if (node.Name == "style")
                {
                    if (string.IsNullOrEmpty(node.InnerText))
                    {
                        if (node.InnerHtml.Contains("expression") || node.InnerHtml.Contains("javascript:"))
                        {
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }

                if (node.HasAttributes)
                {
                    for (int i = node.Attributes.Count - 1; i >= 0; i--)
                    {
                        HtmlAttribute currentAttribute = node.Attributes[i];

                        var attributeName = currentAttribute.Name.ToLower();
                        var attributeValue = (currentAttribute.Value ?? "").ToLower();

                        if (attributeName.StartsWith("on"))
                        {
                            node.Attributes.Remove(currentAttribute);
                        }
                        else if (attributeValue.Contains("script:"))
                        {
                            node.Attributes.Remove(currentAttribute);
                        }
                        else if (attributeName == "style" && attributeValue.Contains("expression") || attributeValue.Contains("script:"))
                        {
                            node.Attributes.Remove(currentAttribute);
                        }
                    }
                }
            }

            if (node.HasChildNodes)
            {
                for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
                {
                    SanitizeHtmlNode(node.ChildNodes[i]);
                }
            }
        }
    }
}
