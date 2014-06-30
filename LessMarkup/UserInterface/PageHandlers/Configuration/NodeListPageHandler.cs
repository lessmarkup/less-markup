/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Configuration;
using LessMarkup.UserInterface.Model.RecordModel;
using LessMarkup.UserInterface.PageHandlers.Common;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.PageHandlers.Configuration
{
    [ConfigurationHandler(CoreTextIds.ViewsTree, ModuleType = ModuleType.Core)]
    public class NodeListPageHandler : AbstractPageHandler, IRecordPageHandler
    {
        private long? _siteId;

        public class LayoutInfo
        {
            public long PageId { get; set; }
            public int Level { get; set; }
        }

        private readonly IModuleIntegration _moduleIntegration;
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly ICurrentUser _currentUser;

        public NodeListPageHandler(IModuleIntegration moduleIntegration, IDataCache dataCache, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ICurrentUser currentUser)
        {
            _moduleIntegration = moduleIntegration;
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _currentUser = currentUser;
        }

        public static string GetHandlerName(Type handlerType, ModuleType moduleType)
        {
            var typeName = handlerType.Name;
            if (typeName.EndsWith("PageHandler"))
            {
                typeName = typeName.Remove(typeName.Length - "PageHandler".Length);
            }
            return typeName + " / " + moduleType;
        }

        public override object GetViewData(long objectId, Dictionary<string, string> settings)
        {
            var modelCache = _dataCache.Get<RecordModelCache>();

            var nodes = new List<NodeSettingsModel>();

            using (var domainModel = _domainModelProvider.Create())
            {
                var previousLevel = -1;

                foreach (var source in domainModel.GetSiteCollection<Page>(_siteId).Select(n => new
                {
                    n.Enabled,
                    n.HandlerId,
                    n.Level,
                    n.Order,
                    n.PageId,
                    n.Path,
                    n.Settings,
                    n.Title
                }).OrderBy(n => n.Order))
                {
                    var node = DependencyResolver.Resolve<NodeSettingsModel>();

                    var handler = source.HandlerId != null ? (IPageHandler) DependencyResolver.Resolve(_moduleIntegration.GetPageHandler(source.HandlerId).Item1) : null;

                    node.Level = source.Level;
                    node.Enabled = source.Enabled;
                    node.HandlerId = source.HandlerId;
                    node.PageId = source.PageId;
                    node.Settings = string.IsNullOrWhiteSpace(source.Settings) ? null : JsonConvert.DeserializeObject(source.Settings);
                    node.Title = source.Title;
                    node.SettingsModelId = (handler != null && handler.SettingsModel != null) ? modelCache.GetDefinition(handler.SettingsModel).Id : null;
                    node.Path = source.Path;
                    node.Customizable = !string.IsNullOrWhiteSpace(node.SettingsModelId);

                    if (previousLevel > node.Level)
                    {
                        previousLevel--;
                        node.Level = previousLevel;
                    }
                    else if (previousLevel < node.Level)
                    {
                        previousLevel++;
                        node.Level = previousLevel;
                    }

                    nodes.Add(node);
                }
            }

            return new
            {
                Nodes = nodes,
                NodeSettingsModelId = modelCache.GetDefinition(typeof(NodeSettingsModel)).Id,
                PageHandlers = _moduleIntegration.GetPageHandlers().Select(id => new { Id = id, Handler = _moduleIntegration.GetPageHandler(id )}).Select(h => new
                {
                    h.Id,
                    Name = GetHandlerName(h.Handler.Item1, h.Handler.Item2),
                })
            };
        }

        public void Initialize(long recordId)
        {
            if (_currentUser.IsGlobalAdministrator)
            {
                _siteId = recordId;
            }
        }

        public object UpdateLayout(List<LayoutInfo> layout)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var nodes = domainModel.GetSiteCollection<Page>(_siteId).ToDictionary(k => k.PageId);

                for (int i = 0; i < layout.Count; i++)
                {
                    var node = nodes[layout[i].PageId];

                    if (node.Order != i || node.Level != layout[i].Level)
                    {
                        node.Order = i;
                        node.Level = layout[i].Level;
                        _changeTracker.AddChange(node.PageId, EntityType.Page, EntityChangeType.Updated, domainModel);
                    }
                }

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return null;
        }

        public object CreateNode(NodeSettingsModel node)
        {
            var modelCache = _dataCache.Get<RecordModelCache>();
            var definition = modelCache.GetDefinition(typeof (NodeSettingsModel));
            definition.ValidateInput(node, true);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var target = new Page
                {
                    Enabled = node.Enabled,
                    HandlerId = node.HandlerId,
                    Level = node.Level,
                    Order = node.Order,
                    Path = node.Path,
                    Settings = node.Settings != null ? JsonConvert.SerializeObject(node.Settings) : null,
                    Title = node.Title
                };

                domainModel.GetSiteCollection<Page>(_siteId).Add(target);
                domainModel.SaveChanges();

                _changeTracker.AddChange(target.PageId, EntityType.Page, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                domainModel.CompleteTransaction();

                node.PageId = target.PageId;

                var handler = (IPageHandler)DependencyResolver.Resolve(_moduleIntegration.GetPageHandler(node.HandlerId).Item1);

                node.Customizable = handler.SettingsModel != null;

                if (node.Customizable)
                {
                    node.SettingsModelId = modelCache.GetDefinition(handler.SettingsModel).Id;
                }

                return node;
            }
        }

        public object DeleteNodes(List<long> ids)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                foreach (var page in domainModel.GetSiteCollection<Page>(_siteId).Where(p => ids.Contains(p.PageId)))
                {
                    domainModel.GetSiteCollection<Page>(_siteId).Remove(page);
                    _changeTracker.AddChange(page.PageId, EntityType.Page, EntityChangeType.Removed, domainModel);
                }

                var pages = domainModel.GetSiteCollection<Page>(_siteId).Where(p => !ids.Contains(p.PageId)).OrderBy(p => p.Order).ToList();

                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i].Order != i)
                    {
                        pages[i].Order = i;
                        _changeTracker.AddChange(pages[i].PageId, EntityType.Page, EntityChangeType.Updated, domainModel);
                    }
                }

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return null;
        }

        public object UpdateNode(NodeSettingsModel node)
        {
            var modelCache = _dataCache.Get<RecordModelCache>();
            var definition = modelCache.GetDefinition(typeof(NodeSettingsModel));
            definition.ValidateInput(node, false);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var page = domainModel.GetSiteCollection<Page>(_siteId).Single(p => p.PageId == node.PageId);

                page.Title = node.Title;
                page.Level = node.Level;
                page.Order = node.Order;
                page.Path = node.Path;
                page.Enabled = node.Enabled;

                _changeTracker.AddChange(node.PageId, EntityType.Page, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return node;
        }

        public object ChangeSettings(long pageId, object settings)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var page = domainModel.GetSiteCollection<Page>(_siteId).Single(p => p.PageId == pageId);

                page.Settings = settings != null ? JsonConvert.SerializeObject(settings) : null;
                _changeTracker.AddChange(pageId, EntityType.Page, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return settings;
        }

        public override bool HasChildren
        {
            get { return true; }
        }

        public override ChildHandlerSettings GetChildHandler(string path)
        {
            var split = path.Split(new[] {'/'});
            long nodeId;
            if (split.Length != 2 || !long.TryParse(split[0], out nodeId) || split[1] != "access")
            {
                return null;
            }

            var handler = DependencyResolver.Resolve<NodeAccessPageHandler>();
            handler.Initialize(_siteId, nodeId);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Path = path,
                Title = "Node Access",
                Id = nodeId
            };
        }
    }
}
