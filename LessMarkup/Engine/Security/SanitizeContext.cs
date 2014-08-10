using System.Collections.Generic;
using DotNetOpenAuth.Messaging;
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
            "meta"
        };

        public SanitizeContext(IEnumerable<string> tagsToRemove)
        {
            if (tagsToRemove != null)
            {
                _blackList.AddRange(tagsToRemove);
            }
        }

        public void Execute(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                // check for blacklist items and remove
                if (_blackList.Contains(node.Name))
                {
                    node.Remove();
                    return;
                }

                if (_blackList.Contains(node.ParentNode.Name + ">" + node.Name))
                {
                    node.Remove();
                    return;
                }

                if (node.Name == "style" && string.IsNullOrEmpty(node.InnerText) && (node.InnerHtml.Contains("expression") || node.InnerHtml.Contains("javascript:")))
                {
                    node.ParentNode.RemoveChild(node);
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
                    Execute(node.ChildNodes[i]);
                }
            }
        }
    }
}
