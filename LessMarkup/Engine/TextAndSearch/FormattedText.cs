/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Engine.TextAndSearch
{
    public static class FormattedText
    {
        private readonly static List<string> _enabledColors = new List<string> { "red", "green", "blue", "yellow", "orange", "white", "black", "purple"};

        private static string GetTagHtml(string name, string argument, string contents, bool isClose)
        {
            switch (name)
            {
                case "b":
                    return isClose ? "</b>" : "<b>";
                case "i":
                    return isClose ? "</i>" : "<i>";
                case "u":
                    return isClose ? "</u>" : "<u>";
                case "quote":
                    return isClose ? "</div></blockquote>" : "<blockquote><div>";
                case "color":
                    if (isClose)
                    {
                        return "</span>";
                    }
                    if (string.IsNullOrEmpty(argument))
                    {
                        return null;
                    }
                    var color = argument.ToLower();
                    if (!_enabledColors.Contains(color))
                    {
                        return null;
                    }
                    return string.Format("<span style=\"color: {0}\">", argument);
                case "img":
                    Uri uri;
                    if (string.IsNullOrEmpty(contents) || !Uri.TryCreate(contents, UriKind.RelativeOrAbsolute, out uri))
                    {
                        return null;
                    }
                    return string.Format("<img src=\"{0}\"/>", uri);
            }
            return null;
        }

        public static string Parse(string taggedText, ILightDomainModel domainModel, UrlHelper urlHelper, ITextTagHandler textTagHandler = null)
        {
            if (taggedText == null)
            {
                return null;
            }

            if (taggedText.Length == 0)
            {
                return taggedText;
            }

            var ret = new StringBuilder();

            var pos = 0;

            taggedText = taggedText.Replace("<", "&lt;");
            taggedText = taggedText.Replace(">", "&gt;");

            while (pos < taggedText.Length)
            {
                var nextPos = taggedText.IndexOf('[', pos);
                if (nextPos < 0)
                {
                    ret.Append(taggedText.Substring(pos));
                    break;
                }

                var nextClosePos = taggedText.IndexOf(']', nextPos);
                if (nextClosePos < 0 || nextClosePos == nextPos+1)
                {
                    ret.Append(taggedText.Substring(pos));
                    break;
                }

                var isValid = true;
                var isClose = false;

                for (var i = nextPos + 1; i < nextClosePos; i++)
                {
                    var c = taggedText[i];

                    if (c == '/')
                    {
                        if (i != nextPos + 1)
                        {
                            isValid = false;
                            break;
                        }
                        isClose = true;
                        continue;
                    }

                    if (char.IsControl(c) || char.IsPunctuation(c) || char.IsSeparator(c) || char.IsWhiteSpace(c))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid)
                {
                    ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                    pos = nextClosePos + 1;
                    continue;
                }

                var tagName = isClose ? taggedText.Substring(nextPos + 2, nextClosePos - 2 - nextPos) : taggedText.Substring(nextPos + 1, nextClosePos - 1 - nextPos);
                var tagArgument = string.Empty;

                var equalPos = tagName.IndexOf('=');
                if (equalPos > 0)
                {
                    if (isClose)
                    {
                        ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                        pos = nextClosePos + 1;
                        continue;
                    }

                    tagArgument = tagName.Substring(equalPos + 1);
                    tagName = tagName.Substring(0, equalPos);
                }

                tagName = tagName.ToLower();

                var validTag = true;
                var requiresCloseTag = false;

                switch (tagName)
                {
                    case "b":
                    case "i":
                    case "u":
                    case "quote":
                        break;
                    case "img":
                        if (isClose)
                        {
                            validTag = false;
                            break;
                        }
                        requiresCloseTag = true;
                        break;
                    case "color":
                        if (!isClose && string.IsNullOrEmpty(tagArgument))
                        {
                            validTag = false;
                        }
                        break;
                    case "size":
                        if (!isClose && string.IsNullOrEmpty(tagArgument))
                        {
                            validTag = false;
                        }
                        break;
                    default:
                        validTag = false;
                        break;
                }

                if (!validTag)
                {
                    ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                    pos = nextClosePos + 1;
                    continue;
                }

                var tagContents = string.Empty;

                var endPos = nextClosePos + 1;

                if (requiresCloseTag)
                {
                    var closeTagPos = taggedText.IndexOf("[/", nextClosePos + 1, StringComparison.Ordinal);

                    if (closeTagPos < 0)
                    {
                        ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                        pos = nextClosePos + 1;
                        continue;
                    }

                    var closeTagClosePos = taggedText.IndexOf(']', closeTagPos);

                    if (closeTagClosePos < 0)
                    {
                        ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                        pos = nextClosePos + 1;
                        continue;
                    }

                    var closeTagName = taggedText.Substring(closeTagPos + 2, closeTagClosePos - closeTagPos - 2).ToLower();

                    if (closeTagName != tagName)
                    {
                        ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                        pos = nextClosePos + 1;
                        continue;
                    }

                    tagContents = taggedText.Substring(nextClosePos + 1, closeTagPos - nextClosePos - 1);

                    endPos = closeTagClosePos + 1;
                }

                string tagHtml = null;

                if (textTagHandler != null)
                {
                    tagHtml = textTagHandler.GetTagHtml(tagName, tagArgument, tagContents, isClose, domainModel, urlHelper);
                }

                if (tagHtml == null)
                {
                    tagHtml = GetTagHtml(tagName, tagArgument, tagContents, isClose);
                }

                if (tagHtml == null)
                {
                    ret.Append(taggedText.Substring(pos, nextClosePos + 1 - pos));
                    pos = nextClosePos + 1;
                    continue;
                }

                if (pos < nextPos)
                {
                    ret.Append(taggedText.Substring(pos, nextPos - pos));
                }

                ret.Append(tagHtml);

                pos = endPos;
            }

            var text = ret.ToString();

            /*foreach (var smile in domainModel.GetSiteCollection<Smile>().OrderBy(s => s.Order).Select(s => new {s.SmileId, s.Code, s.Name}))
            {
                var smilePos = text.IndexOf(smile.Code, StringComparison.Ordinal);
                if (smilePos < 0)
                {
                    continue;
                }
                var replacement = string.Format("<img src=\"{0}\" alt=\"{1}\"/>", urlHelper.Smile(smile.SmileId), smile.Name);
                text = text.Replace(smile.Code, replacement);
            }*/

            return text;
        }
    }
}
