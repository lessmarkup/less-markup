/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web;

namespace LessMarkup.Engine.Helpers
{
    public static class TextSearchHelper
    {
        public static IHtmlString HighlightHtmlText(string text, string highlight)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HtmlString("");
            }

            if (string.IsNullOrWhiteSpace(highlight))
            {
                return new HtmlString(text);
            }

            highlight = HttpUtility.HtmlEncode(highlight);

            string ret = "";
            for (; ; )
            {
                int pos = text.IndexOf(highlight, System.StringComparison.InvariantCultureIgnoreCase);
                if (pos < 0)
                {
                    break;
                }
                if (pos > 0)
                {
                    ret += text.Substring(0, pos);
                }
                ret += "<mark>";
                ret += text.Substring(pos, highlight.Length);
                ret += "</mark>";
                text = text.Remove(0, pos + highlight.Length);
            }

            return new HtmlString(ret + text);
        }

        public static IHtmlString HighlightText(string text, string highlight)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HtmlString("");
            }

            text = HttpUtility.HtmlEncode(text);

            return HighlightHtmlText(text, highlight);
        }
    }
}
