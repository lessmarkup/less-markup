/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.Engine.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Structure;

namespace LessMarkup.UserInterface.NodeHandlers.Configuration
{
    public class ConfigurationRootNodeHandler : AbstractNodeHandler
    {
        class ConfigurationGroup
        {
            public string Title { get; set; }
            public List<ConfigurationHandler> Handlers { get; set; }
        }

        class ConfigurationHandler
        {
            public Type Type { get; set; }
            public object TitleTextId { get; set; }
            public ModuleType ModuleType { get; set; }
            public long Id { get; set; }
            public string TypeName { get; set; }
        }

        private readonly List<ConfigurationGroup> _configurationGroups = new List<ConfigurationGroup>(); 
        private readonly Dictionary<string, ConfigurationHandler> _configurationHandlers = new Dictionary<string, ConfigurationHandler>();
        private readonly IDataCache _dataCache;

        public ConfigurationRootNodeHandler(IModuleProvider moduleProvider, IDataCache dataCache, ISiteMapper siteMapper, ICurrentUser currentUser)
        {
            _dataCache = dataCache;

            bool addSiteHandlers = siteMapper.SiteId.HasValue;
            bool addGlobalHandlers = currentUser.IsGlobalAdministrator;

            long idCounter = 1;

            _configurationGroups = new List<ConfigurationGroup>();

            var normalGroup = new ConfigurationGroup
            {
                Handlers = new List<ConfigurationHandler>(),
                Title = LanguageHelper.GetText(ModuleType.UserInterface, UserInterfaceTextIds.SiteConfiguration)
            };

            var globalGroup = new ConfigurationGroup
            {
                Handlers = new List<ConfigurationHandler>(),
                Title = LanguageHelper.GetText(ModuleType.UserInterface, UserInterfaceTextIds.GlobalConfiguration)
            };

            foreach (var module in moduleProvider.Modules)
            {
                foreach (var type in module.Assembly.GetTypes())
                {
                    var configurationHandlerAttribute = type.GetCustomAttribute<ConfigurationHandlerAttribute>();
                    if (configurationHandlerAttribute == null)
                    {
                        continue;
                    }
                    if (!typeof (INodeHandler).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (configurationHandlerAttribute.IsGlobal)
                    {
                        switch (module.ModuleType)
                        {
                            case ModuleType.MainModule:
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
                    if (typeName.EndsWith("nodehandler"))
                    {
                        typeName = typeName.Remove(typeName.Length - "nodehandler".Length);
                    }

                    var handler = new ConfigurationHandler
                    {
                        Type = type,
                        ModuleType =
                            configurationHandlerAttribute.ModuleType == ModuleType.None
                                ? module.ModuleType
                                : configurationHandlerAttribute.ModuleType,
                        TitleTextId = configurationHandlerAttribute.TitleTextId,
                        Id = idCounter++,
                        TypeName = typeName
                    };

                    _configurationHandlers[typeName] = handler;

                    if (configurationHandlerAttribute.IsGlobal)
                    {
                        globalGroup.Handlers.Add(handler);
                    }
                    else
                    {
                        normalGroup.Handlers.Add(handler);
                    }
                }
            }

            if (globalGroup.Handlers.Any())
            {
                _configurationGroups.Add(globalGroup);
            }

            if (normalGroup.Handlers.Any())
            {
                _configurationGroups.Add(normalGroup);
            }
        }

        public override bool HasChildren
        {
            get { return true; }
        }

        public override object GetViewData(long objectId, object settings, object controller)
        {
            var path = _dataCache.Get<NodeCache>().GetNode(objectId).FullPath;

            return new
            {
                Groups = _configurationGroups.Select(g => new
                {
                    g.Title,

                    Items = g.Handlers.OrderBy(h => h.TitleTextId.ToString()).Select(h => new
                    {
                        Path = path + "/" + h.TypeName,
                        Title = LanguageHelper.GetText(h.ModuleType, h.TitleTextId)
                    }).ToList()
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

            var handler = (INodeHandler) DependencyResolver.Resolve(handlerData.Type);

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
