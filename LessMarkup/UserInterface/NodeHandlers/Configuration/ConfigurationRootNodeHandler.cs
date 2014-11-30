/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;

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
            public string Title { get; set; }
            public string ModuleType { get; set; }
            public long Id { get; set; }
            public string TypeName { get; set; }
        }

        private readonly List<ConfigurationGroup> _configurationGroups = new List<ConfigurationGroup>(); 
        private readonly Dictionary<string, ConfigurationHandler> _configurationHandlers = new Dictionary<string, ConfigurationHandler>();
        private readonly IDataCache _dataCache;

        public ConfigurationRootNodeHandler(IModuleProvider moduleProvider, IDataCache dataCache)
        {
            AddScript("controllers/configuration");

            _dataCache = dataCache;

            long idCounter = 1;

            _configurationGroups = new List<ConfigurationGroup>();

            var normalGroup = new ConfigurationGroup
            {
                Handlers = new List<ConfigurationHandler>(),
                Title = LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.SiteConfiguration)
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

                    var typeName = type.Name.ToLower();
                    if (typeName.EndsWith("nodehandler"))
                    {
                        typeName = typeName.Remove(typeName.Length - "nodehandler".Length);
                    }

                    var handler = new ConfigurationHandler
                    {
                        Type = type,
                        ModuleType = module.ModuleType,
                        TitleTextId = configurationHandlerAttribute.TitleTextId,
                        Id = idCounter++,
                        TypeName = typeName
                    };

                    _configurationHandlers[typeName] = handler;

                    normalGroup.Handlers.Add(handler);
                }
            }

            if (normalGroup.Handlers.Any())
            {
                _configurationGroups.Add(normalGroup);
            }

            foreach (var handler in _configurationHandlers.Values)
            {
                handler.Title = LanguageHelper.GetText(handler.ModuleType, handler.TitleTextId);
            }

            foreach (var group in _configurationGroups)
            {
                group.Handlers.Sort((h1, h2) => String.Compare(h1.Title, h2.Title, StringComparison.Ordinal));
            }
        }

        protected override bool HasChildren
        {
            get { return true; }
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var path = ObjectId.HasValue ? _dataCache.Get<INodeCache>().GetNode(ObjectId.Value).FullPath : null;

            return new Dictionary<string, object>
            {
                { "Groups", _configurationGroups.Select(g => new
                {
                    g.Title,

                    Items = g.Handlers.OrderBy(h => h.TitleTextId.ToString()).Select(h => new
                    {
                        Path = path != null ? (path + "/" + h.TypeName) : h.TypeName,
                        Title = LanguageHelper.GetText(h.ModuleType, h.TitleTextId)
                    }).ToList()
                }).ToList() }
            };
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
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

            handler.Initialize(null, null, null, path, FullPath + "/" + path, NodeAccessType.Read);

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
