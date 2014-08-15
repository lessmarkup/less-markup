/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Reflection;
using System.Web;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Email
{
    class MailTemplateProvider : IMailTemplateProvider
    {
        private readonly IDataCache _dataCache;
        private readonly IHtmlSanitizer _htmlSanitizer;

        public MailTemplateProvider(IDataCache dataCache, IHtmlSanitizer htmlSanitizer)
        {
            _dataCache = dataCache;
            _htmlSanitizer = htmlSanitizer;
        }

        public string ExecuteTemplate<T>(string viewPath, T model) where T : MailTemplateModel
        {
            var resourceCache = _dataCache.Get<ResourceCache>();

            var template = resourceCache.ReadText(viewPath);

            var pos = 0;

            for (;;)
            {
                pos = template.IndexOf('{', pos);
                if (pos < 0)
                {
                    break;
                }

                if (pos > 0 && template[pos - 1] == '\\')
                {
                    pos++;
                    continue;
                }

                var end = template.IndexOf('}', pos + 1);

                if (end < 0)
                {
                    break;
                }

                var encodeHtml = true;
                var encodeUrl = false;

                var parameter = template.Substring(pos + 1, end - pos - 1);

                if (parameter.StartsWith("!"))
                {
                    parameter = parameter.Remove(0, 1);
                    encodeHtml = false;
                }
                else if (parameter.StartsWith("$"))
                {
                    parameter = parameter.Remove(0, 1);
                    encodeHtml = false;
                    encodeUrl = true;
                }

                if (string.IsNullOrEmpty(parameter) || parameter.Any(c => !Char.IsLetterOrDigit(c) && c != '_'))
                {
                    pos++;
                    continue;
                }

                var property = typeof(T).GetProperty(parameter, BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                {
                    throw new Exception("Unknown property " + parameter);
                }

                var value = property.GetValue(model).ToString();

                if (encodeHtml)
                {
                    value = HttpUtility.HtmlEncode(value);
                }
                else if (encodeUrl)
                {
                    value = HttpUtility.UrlEncode(value) ?? "";
                }
                else
                {
                    value = _htmlSanitizer.Sanitize(value);
                }

                template = template.Substring(0, pos) + value + template.Substring(end + 1);

                pos += value.Length;
            }

            var lines = template.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

            while (lines.Count > 0)
            {
                var line = lines[0];

                if (string.IsNullOrWhiteSpace(line))
                {
                    lines.RemoveAt(0);
                    break;
                }

                var delimiter = line.IndexOf(':');

                if (delimiter > 0)
                {
                    var name = line.Substring(0, delimiter).Trim();
                    var value = line.Substring(delimiter + 1).Trim();

                    var property = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

                    if (property == null)
                    {
                        throw new Exception("Unknown property " + name);
                    }

                    property.SetValue(model, value);
                }

                lines.RemoveAt(0);
            }

            template = string.Join("\r\n", lines);

            return template;
        }
    }
}
