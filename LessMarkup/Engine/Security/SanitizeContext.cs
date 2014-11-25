/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Xml.XPath;
using HtmlAgilityPack;

namespace LessMarkup.Engine.Security
{
    class SanitizeContext
    {
        private readonly HashSet<string> _blackList = new HashSet<string>
        {
            "script",
            "iframe",
            "form",
            "object",
            "embed",
            "link",
            "head",
            "meta",
            "input",
            "button",
            "style"
        };

        private readonly Func<IXPathNavigable, bool?> _validateFunc;

        public SanitizeContext(IEnumerable<string> tagsToRemove = null, Func<IXPathNavigable, bool?> validateFunc = null)
        {
            if (tagsToRemove != null)
            {
                foreach (var item in tagsToRemove)
                {
                    _blackList.Add(item);
                }
            }

            _validateFunc = validateFunc;
        }

        private void RemoveSuspiciousAttributes(HtmlNode node)
        {
            if (!node.HasAttributes)
            {
                return;
            }

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

        private bool ValidateNode(HtmlNode node)
        {
            // check for blacklist items and remove
            if (_blackList.Contains(node.Name))
            {
                return false;
            }

            if (_blackList.Contains(node.ParentNode.Name + ">" + node.Name))
            {
                return false;
            }

            if (node.Name == "style" && string.IsNullOrEmpty(node.InnerText) && (node.InnerHtml.Contains("expression") || node.InnerHtml.Contains("javascript:")))
            {
                return false;
            }

            return true;
        }

        public void Execute(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                bool? ret = null;

                if (_validateFunc != null)
                {
                    ret = _validateFunc(node);

                    if (ret.HasValue)
                    {
                        if (!ret.Value)
                        {
                            node.Remove();
                            return;
                        }
                    }
                }

                if (!ret.HasValue)
                {
                    ret = ValidateNode(node);
                }

                if (!ret.Value)
                {
                    node.Remove();
                    return;
                }

                RemoveSuspiciousAttributes(node);
            }

            if (node.HasChildNodes)
            {
                for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
                {
                    Execute(node.ChildNodes[i]);
                }
            }
        }
    }
}
