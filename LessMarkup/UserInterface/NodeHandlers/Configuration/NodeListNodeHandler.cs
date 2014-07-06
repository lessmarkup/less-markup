/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Configuration;
using LessMarkup.UserInterface.Model.RecordModel;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.NodeHandlers.Configuration
{
    [ConfigurationHandler(MainModuleTextIds.ViewsTree, ModuleType = ModuleType.MainModule)]
    public class NodeListNodeHandler : AbstractNodeHandler, IRecordNodeHandler
    {
        private long? _siteId;

        public class LayoutInfo
        {
            public long NodeId { get; set; }
            public int Level { get; set; }
        }

        private readonly IModuleIntegration _moduleIntegration;
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly ICurrentUser _currentUser;

        public NodeListNodeHandler(IModuleIntegration moduleIntegration, IDataCache dataCache, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ICurrentUser currentUser)
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
            if (typeName.EndsWith("NodeHandler"))
            {
                typeName = typeName.Remove(typeName.Length - "NodeHandler".Length);
            }
            return typeName + " / " + moduleType;
        }

        private IDataObjectCollection<Node> GetNodeCollection(IDomainModel domainModel)
        {
            return _siteId.HasValue
                ? domainModel.GetSiteCollection<Node>(_siteId.Value)
                : domainModel.GetSiteCollection<Node>();
        }

        public override object GetViewData(long objectId, object settings, object controller)
        {
            var modelCache = _dataCache.Get<RecordModelCache>();

            var nodes = new List<NodeSettingsModel>();

            using (var domainModel = _domainModelProvider.Create())
            {
                var previousLevel = -1;

                foreach (var source in GetNodeCollection(domainModel).Select(n => new
                {
                    n.Enabled,
                    n.HandlerId,
                    n.Level,
                    n.Order,
                    n.NodeId,
                    n.Path,
                    n.Settings,
                    n.Title
                }).OrderBy(n => n.Order))
                {
                    var node = DependencyResolver.Resolve<NodeSettingsModel>();

                    var handler = source.HandlerId != null ? (INodeHandler) DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(source.HandlerId).Item1) : null;

                    node.Level = source.Level;
                    node.Enabled = source.Enabled;
                    node.HandlerId = source.HandlerId;
                    node.NodeId = source.NodeId;
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
                NodeHandlers = _moduleIntegration.GetNodeHandlers().Select(id => new { Id = id, Handler = _moduleIntegration.GetNodeHandler(id )}).Select(h => new
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
                var nodes = GetNodeCollection(domainModel).ToDictionary(k => k.NodeId);

                for (int i = 0; i < layout.Count; i++)
                {
                    var node = nodes[layout[i].NodeId];

                    if (node.Order != i || node.Level != layout[i].Level)
                    {
                        node.Order = i;
                        node.Level = layout[i].Level;
                        _changeTracker.AddChange(node.NodeId, EntityType.Node, EntityChangeType.Updated, domainModel);
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
                var target = new Node
                {
                    Enabled = node.Enabled,
                    HandlerId = node.HandlerId,
                    Level = node.Level,
                    Order = node.Order,
                    Path = node.Path,
                    Settings = node.Settings != null ? JsonConvert.SerializeObject(node.Settings) : null,
                    Title = node.Title
                };

                GetNodeCollection(domainModel).Add(target);
                domainModel.SaveChanges();

                _changeTracker.AddChange(target.NodeId, EntityType.Node, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                domainModel.CompleteTransaction();

                node.NodeId = target.NodeId;

                var handler = (INodeHandler)DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(node.HandlerId).Item1);

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
                foreach (var node in GetNodeCollection(domainModel).Where(p => ids.Contains(p.NodeId)))
                {
                    GetNodeCollection(domainModel).Remove(node);
                    _changeTracker.AddChange(node.NodeId, EntityType.Node, EntityChangeType.Removed, domainModel);
                }

                var nodes = domainModel.GetSiteCollection<Node>(_siteId).Where(p => !ids.Contains(p.NodeId)).OrderBy(p => p.Order).ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Order != i)
                    {
                        nodes[i].Order = i;
                        _changeTracker.AddChange(nodes[i].NodeId, EntityType.Node, EntityChangeType.Updated, domainModel);
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
                var record = GetNodeCollection(domainModel).Single(p => p.NodeId == node.NodeId);

                record.Title = node.Title;
                record.Level = node.Level;
                record.Order = node.Order;
                record.Path = node.Path;
                record.Enabled = node.Enabled;

                _changeTracker.AddChange(node.NodeId, EntityType.Node, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return node;
        }

        public object ChangeSettings(long nodeId, object settings)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = GetNodeCollection(domainModel).Single(p => p.NodeId == nodeId);

                node.Settings = settings != null ? JsonConvert.SerializeObject(settings) : null;
                _changeTracker.AddChange(nodeId, EntityType.Node, EntityChangeType.Updated, domainModel);

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

            var handler = DependencyResolver.Resolve<NodeAccessNodeHandler>();
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
