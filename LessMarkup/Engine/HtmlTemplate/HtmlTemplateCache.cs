/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Engine.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.HtmlTemplate
{
    public class HtmlTemplateCache : ICacheHandler
    {
        private readonly IDataCache _dataCache;
        private readonly Dictionary<string, CacheItem> _cacheItems = new Dictionary<string, CacheItem>();
        private readonly object _syncLock = new object();

        public HtmlTemplateCache(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        private CacheItem CreateCacheItem(ResourceReference reference, string path)
        {
            var cacheItem = new CacheItem
            {
                TextParts = new List<string>(),
                Directives = new List<Directive>(),
                Path = path
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
                        if (level > 3)
                        {
                            break;
                        }

                        string path;

                        if (!directive.Body.Contains("/"))
                        {
                            path = cacheItem.Path;
                            var index = path.LastIndexOf("/", StringComparison.Ordinal);
                            if (index > 0)
                            {
                                path = path.Substring(0, index + 1);
                            }
                            else
                            {
                                path = "";
                            }

                            path += directive.Body;
                        }
                        else
                        {
                            path = directive.Body;
                        }

                        var childItem = GetCacheItem(path);

                        if (childItem != null)
                        {
                            BuildTemplate(builder, childItem, level + 1);
                        }

                        break;
                    case DirectiveType.Translate:

                        var pos = directive.Body.IndexOf("/", StringComparison.Ordinal);
                        if (pos <= 0)
                        {
                            break;
                        }
                        ModuleType moduleType;
                        if (!Enum.TryParse(directive.Body.Substring(0, pos), true, out moduleType))
                        {
                            break;
                        }
                        var text = LanguageHelper.GetText(moduleType, directive.Body.Substring(pos + 1));
                        builder.Append(text);
                        break;
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

        public void Initialize(out DateTime? expirationTime, long? objectId = null)
        {
            expirationTime = null;
        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.SiteCustomization;
        }

        private readonly EntityType[] _entityTypes = { EntityType.SiteCustomization};

        public EntityType[] HandledTypes { get { return _entityTypes; } }
    }
}
