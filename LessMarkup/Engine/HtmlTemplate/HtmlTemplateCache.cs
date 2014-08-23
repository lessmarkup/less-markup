/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Configuration;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.HtmlTemplate
{
    public class HtmlTemplateCache : AbstractCacheHandler
    {
        private readonly IDataCache _dataCache;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly Dictionary<string, CacheItem> _cacheItems = new Dictionary<string, CacheItem>();
        private readonly object _syncLock = new object();

        public HtmlTemplateCache(IDataCache dataCache, IEngineConfiguration engineConfiguration) : base(new[] { typeof(SiteCustomization) })
        {
            _dataCache = dataCache;
            _engineConfiguration = engineConfiguration;
        }

        private CacheItem CreateCacheItem(ResourceReference reference, string path)
        {
            var cacheItem = new CacheItem
            {
                TextParts = new List<string>(),
                Directives = new List<Directive>(),
                Path = path,
                ModuleType = reference.ModuleType
            };

            var body = Encoding.UTF8.GetString(reference.Binary);

            int offset;

            for (offset = 0; offset < body.Length;)
            {
                var start = body.IndexOf("[[[", offset, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                var end = body.IndexOf("]]]", start + 4, StringComparison.Ordinal);
                if (end <= start)
                {
                    offset = start + 3;
                    continue;
                }

                var command = body.Substring(start + 3, end - start - 3);
                var pos = command.IndexOf(' ');

                end += 3;

                if (pos <= 0)
                {
                    offset = end;
                    continue;
                }

                DirectiveType directiveType;
                if (!Enum.TryParse(command.Substring(0, pos), true, out directiveType))
                {
                    offset = end;
                    continue;
                }

                var directive = new Directive
                {
                    Type = directiveType,
                    Body = command.Substring(pos + 1)
                };

                cacheItem.Directives.Add(directive);
                cacheItem.TextParts.Add(body.Substring(offset, start-offset));

                offset = end;
            }

            if (offset < body.Length)
            {
                cacheItem.TextParts.Add(body.Substring(offset));
            }

            return cacheItem;
        }

        private string GetPropertyValue(string name)
        {
            var property = typeof(ISiteConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Compare(p.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (property != null)
            {
                return property.GetValue(_dataCache.Get<ISiteConfiguration>()).ToString();
            }

            property = typeof(EngineConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Compare(p.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (property != null)
            {
                return property.GetValue(_engineConfiguration).ToString();
            }

            return null;
        }

        private void BuildTemplate(StringBuilder builder, CacheItem cacheItem, int level = 0)
        {
            int i;
            for (i = 0; i < cacheItem.Directives.Count; i++)
            {
                builder.Append(cacheItem.TextParts[i]);
                var directive = cacheItem.Directives[i];
                switch (directive.Type)
                {
                    case DirectiveType.Include:
                    case DirectiveType.IncludeIf:
                    {
                        if (level > 4)
                        {
                            break;
                        }

                        string path;
                        var directiveBody = directive.Body;

                        if (directive.Type == DirectiveType.IncludeIf)
                        {
                            var pos = directiveBody.IndexOf(',');
                            if (pos <= 0)
                            {
                                break;
                            }

                            var condition = directiveBody.Substring(pos).Trim();
                            directiveBody = directiveBody.Substring(pos + 1).Trim();

                            var conditionParts = condition.Split(new[] {'-'});

                            if (conditionParts.Length > 2)
                            {
                                break;
                            }

                            var value = GetPropertyValue(conditionParts[0]);

                            if (value == null)
                            {
                                break;
                            }

                            if (conditionParts.Length == 1)
                            {
                                if (value != true.ToString())
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (value != conditionParts[1].Trim())
                                {
                                    break;
                                }
                            }
                        }

                        if (!directiveBody.Contains("/"))
                        {
                            path = cacheItem.Path;
                            var index = path.LastIndexOf("/", StringComparison.Ordinal);
                            path = index > 0 ? path.Substring(0, index + 1) : "";

                            path += directiveBody;
                        }
                        else
                        {
                            path = directiveBody;
                        }

                        var childItem = GetCacheItem(path);

                        if (childItem != null)
                        {
                            BuildTemplate(builder, childItem, level + 1);
                        }

                        break;
                    }
                    case DirectiveType.Translate:
                    {
                        string moduleType;
                        string text;
                        var pos = directive.Body.IndexOf("/", StringComparison.Ordinal);
                        if (pos <= 0)
                        {
                            moduleType = cacheItem.ModuleType;
                            text = directive.Body;
                        }
                        else
                        {
                            moduleType = directive.Body.Substring(0, pos);
                            text = directive.Body.Substring(pos + 1);
                        }
                        text = LanguageHelper.GetText(moduleType, text);
                        builder.Append(text);
                        break;
                    }
                    case DirectiveType.Parameter:
                    {
                        var value = GetPropertyValue(directive.Body);

                        if (value == null)
                        {
                            break;
                        }

                        builder.Append(value);
                        break;
                    }
                }
            }

            if (i < cacheItem.TextParts.Count)
            {
                builder.Append(cacheItem.TextParts[i]);
            }
        }

        private CacheItem GetCacheItem(string path)
        {
            CacheItem cacheItem;

            lock (_syncLock)
            {
                if (_cacheItems.TryGetValue(path, out cacheItem))
                {
                    return cacheItem;
                }

                var resourceCache = _dataCache.Get<ResourceCache>();
                var reference = resourceCache.GetResourceReference(path);
                if (reference == null || reference.Binary == null)
                {
                    return null;
                }
                cacheItem = CreateCacheItem(reference, path);
                _cacheItems[path] = cacheItem;
            }

            return cacheItem;
        }

        public string GetTemplate(string path)
        {
            var cacheItem = GetCacheItem(path);

            if (cacheItem == null)
            {
                return null;
            }

            var builder = new StringBuilder();

            BuildTemplate(builder, cacheItem);

            return builder.ToString().Trim();
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
        }
    }
}
