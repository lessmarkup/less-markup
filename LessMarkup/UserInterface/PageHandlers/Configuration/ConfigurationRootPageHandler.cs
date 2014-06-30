/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Structure;
using LessMarkup.UserInterface.PageHandlers.Common;

namespace LessMarkup.UserInterface.PageHandlers.Configuration
{
    public class ConfigurationRootPageHandler : AbstractPageHandler
    {
        class ConfigurationHandler
        {
            public Type Type { get; set; }
            public object TitleTextId { get; set; }
            public ModuleType ModuleType { get; set; }
            public long Id { get; set; }
        }

        private readonly Dictionary<string, ConfigurationHandler> _configurationHandlers = new Dictionary<string, ConfigurationHandler>();
        private readonly IDataCache _dataCache;

        public ConfigurationRootPageHandler(IModuleProvider moduleProvider, IDataCache dataCache, ISiteMapper siteMapper, ICurrentUser currentUser)
        {
            _dataCache = dataCache;

            bool addSiteHandlers = siteMapper.SiteId.HasValue;
            bool addGlobalHandlers = currentUser.IsGlobalAdministrator;

            long idCounter = 1;

            foreach (var module in moduleProvider.Modules)
            {
                foreach (var type in module.Assembly.GetTypes())
                {
                    var configurationHandlerAttribute = type.GetCustomAttribute<ConfigurationHandlerAttribute>();
                    if (configurationHandlerAttribute == null)
                    {
                        continue;
                    }
                    if (!typeof (IPageHandler).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (configurationHandlerAttribute.IsGlobal)
                    {
                        switch (module.ModuleType)
                        {
                            case ModuleType.Core:
                            case ModuleType.UserInterface:
                                break;
                            default:
                                continue;
                        }

                        if (!addGlobalHandlers)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!addSiteHandlers)
                        {
                            continue;
                        }
                    }

                    var typeName = type.Name.ToLower();
                    if (typeName.EndsWith("pagehandler"))
                    {
                        typeName = typeName.Remove(typeName.Length - "pagehandler".Length);
                    }

                    _configurationHandlers[typeName] = new ConfigurationHandler
                    {
                        Type = type,
                        ModuleType = configurationHandlerAttribute.ModuleType == ModuleType.None ? module.ModuleType : configurationHandlerAttribute.ModuleType,
                        TitleTextId = configurationHandlerAttribute.TitleTextId,
                        Id = idCounter++
                    };
                }
            }
        }

        public override bool HasChildren
        {
            get { return true; }
        }

        public override bool IsStatic
        {
            get { return true; }
        }

        public override object GetViewData(long objectId, Dictionary<string, string> settings)
        {
            var path = _dataCache.Get<PageCache>().GetPage(objectId).FullPath;

            return new
            {
                Items = _configurationHandlers.Select(h => new
                {
                    Path = path + "/" + h.Key,
                    Title = LanguageHelper.GetText(h.Value.ModuleType, h.Value.TitleTextId)
                }).ToList()
            };
        }

        public override ChildHandlerSettings GetChildHandler(string path)
        {
            var parts = path.Split(new[] {'/'}).Select(p => p.Trim()).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            ConfigurationHandler handlerData;
            if (!_configurationHandlers.TryGetValue(parts[0], out handlerData))
            {
                return null;
            }

            var handler = (IPageHandler) DependencyResolver.Resolve(handlerData.Type);

            path = parts[0];
            parts.RemoveAt(0);

            return new ChildHandlerSettings
            {
                Id = handlerData.Id,
                Handler = handler,
                Title = LanguageHelper.GetText(handlerData.ModuleType, handlerData.TitleTextId),
                Path = path,
                Rest = string.Join("/", parts)
            };
        }
    }
}
