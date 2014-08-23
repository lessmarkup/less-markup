/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Configuration;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.NodeHandlers.Configuration
{
    [ConfigurationHandler(UserInterfaceTextIds.ViewsTree)]
    public class NodeListNodeHandler : AbstractNodeHandler
    {
        public class LayoutInfo
        {
            public long NodeId { get; set; }
            public int Level { get; set; }
        }

        private readonly IModuleIntegration _moduleIntegration;
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly ISiteMapper _siteMapper;

        private long SiteId
        {
            get
            {
                var ret = ObjectId ?? _siteMapper.SiteId;
                if (!ret.HasValue)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return ret.Value;
            }
        }

        public NodeListNodeHandler(IModuleIntegration moduleIntegration, IDataCache dataCache, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ISiteMapper siteMapper)
        {
            _moduleIntegration = moduleIntegration;
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _siteMapper = siteMapper;
            AddScript("controllers/nodelist");
        }

        public static string GetHandlerName(Type handlerType, string moduleType)
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
            return ObjectId.HasValue
                ? domainModel.GetSiteCollection<Node>(ObjectId.Value)
                : domainModel.GetSiteCollection<Node>();
        }

        private NodeSettingsModel GetRootNode()
        {
            NodeSettingsModel rootNode;
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var nodes = new List<NodeSettingsModel>();
            var nodeIds = new Dictionary<long, NodeSettingsModel>();

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var source in GetNodeCollection(domainModel).Select(n => new
                {
                    n.Enabled,
                    n.HandlerId,
                    n.ParentId,
                    n.Id,
                    n.Path,
                    n.Settings,
                    n.Title,
                    n.Order
                }).OrderBy(n => n.Order))
                {
                    var node = DependencyResolver.Resolve<NodeSettingsModel>();

                    var handler = source.HandlerId != null ? (INodeHandler)DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(source.HandlerId).Item1) : null;

                    node.ParentId = source.ParentId;
                    node.Enabled = source.Enabled;
                    node.HandlerId = source.HandlerId;
                    node.NodeId = source.Id;
                    node.Order = source.Order;
                    node.Settings = string.IsNullOrWhiteSpace(source.Settings) ? null : JsonConvert.DeserializeObject(source.Settings);
                    node.Title = source.Title;
                    node.SettingsModelId = (handler != null && handler.SettingsModel != null) ? modelCache.GetDefinition(handler.SettingsModel).Id : null;
                    node.Path = source.Path;
                    node.Customizable = !string.IsNullOrWhiteSpace(node.SettingsModelId);

                    nodes.Add(node);
                    nodeIds[node.NodeId] = node;
                }

                var changedNodes = new HashSet<long>();

                foreach (var node in nodes)
                {
                    if (!node.ParentId.HasValue)
                    {
                        continue;
                    }

                    NodeSettingsModel parent;
                    if (!nodeIds.TryGetValue(node.ParentId.Value, out parent))
                    {
                        node.ParentId = null;
                        var record = GetNodeCollection(domainModel).First(n => n.Id == node.NodeId);
                        record.ParentId = null;
                        changedNodes.Add(node.NodeId);
                    }
                    else
                    {
                        parent.Children.Add(node);
                    }
                }

                rootNode = nodes.FirstOrDefault(n => !n.ParentId.HasValue);

                NormalizeTree(nodes, rootNode, domainModel, changedNodes);

                if (changedNodes.Count > 0)
                {
                    foreach (var nodeId in changedNodes)
                    {
                        _changeTracker.AddChange<Node>(nodeId, EntityChangeType.Updated, domainModel);
                    }
                    domainModel.SaveChanges();
                }
            }

            return rootNode;
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();

            return new Dictionary<string, object>
            {
                { "Root", GetRootNode() },
                { "NodeSettingsModelId", modelCache.GetDefinition(typeof(NodeSettingsModel)).Id },
                { "NodeHandlers", _moduleIntegration.GetNodeHandlers().Select(id => new { Id = id, Handler = _moduleIntegration.GetNodeHandler(id )}).Select(h => new
                {
                    h.Id,
                    Name = GetHandlerName(h.Handler.Item1, h.Handler.Item2),
                })}
            };
        }

        private void NormalizeTree(List<NodeSettingsModel> nodes, NodeSettingsModel rootNode, IDomainModel domainModel, HashSet<long> changedNodes)
        {
            foreach (var node in nodes)
            {
                node.Children.Sort((n1, n2) => n1.Order.CompareTo(n2.Order));
            }

            if (rootNode != null)
            {
                foreach (var node in nodes.Where(n => n.NodeId != rootNode.NodeId && !n.ParentId.HasValue))
                {
                    rootNode.Children.Add(node);
                    node.ParentId = rootNode.NodeId;
                    var record = GetNodeCollection(domainModel).First(n => n.Id == node.NodeId);
                    record.ParentId = node.ParentId;
                    changedNodes.Add(node.NodeId);
                }
            }

            foreach (var node in nodes)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];

                    if (child.Order != i)
                    {
                        child.Order = i;
                        var record = GetNodeCollection(domainModel).First(n => n.Id == child.NodeId);
                        record.Order = i;
                        changedNodes.Add(record.Id);
                    }
                }
            }
        }

        public object UpdateParent(long nodeId, long? parentId, int order)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = GetNodeCollection(domainModel).First(n => n.Id == nodeId);

                var changedNodes = new HashSet<long>();

                if (node.ParentId.HasValue != parentId.HasValue) // we exchange root node and specified node
                {
                    Node newRootNode;
                    Node oldRootNode;

                    if (node.ParentId.HasValue)
                    {
                        newRootNode = node;
                        oldRootNode = GetNodeCollection(domainModel).First(n => !n.ParentId.HasValue);
                    }
                    else
                    {
                        oldRootNode = node;
                        newRootNode = GetNodeCollection(domainModel).First(n => n.Id == parentId.Value);
                    }

                    if (!newRootNode.ParentId.HasValue)
                    {
                        throw new Exception("Cannot have two root nodes");
                    }

                    foreach (var neighbor in GetNodeCollection(domainModel).Where(n => n.ParentId == newRootNode.ParentId.Value && n.Order > newRootNode.Order))
                    {
                        neighbor.Order--;
                        changedNodes.Add(neighbor.Id);
                    }

                    newRootNode.Order = 0;
                    newRootNode.ParentId = null;
                    changedNodes.Add(newRootNode.Id);

                    foreach (var neighbor in GetNodeCollection(domainModel).Where(n => n.ParentId == newRootNode.Id && n.Order >= 0 && n.Id != nodeId))
                    {
                        neighbor.Order++;
                        changedNodes.Add(neighbor.Id);
                    }

                    oldRootNode.Order = 0;
                    oldRootNode.ParentId = newRootNode.Id;
                    changedNodes.Add(oldRootNode.Id);
                }
                else if (node.ParentId.HasValue)
                {
                    if (node.ParentId == parentId)
                    {
                        if (order > node.Order)
                        {
                            foreach (var neighbor in GetNodeCollection(domainModel).Where(n => n.ParentId == parentId && n.Id != nodeId && n.Order > node.Order && n.Order <= order))
                            {
                                neighbor.Order--;
                                changedNodes.Add(neighbor.Id);
                            }
                        }
                        else if (order < node.Order)
                        {
                            foreach (var neighbor in GetNodeCollection(domainModel).Where(n => n.ParentId == parentId && n.Id != nodeId && n.Order >= order && n.Order < node.Order))
                            {
                                neighbor.Order++;
                                changedNodes.Add(neighbor.Id);
                            }
                        }
                    }
                    else
                    { 
                        foreach (var neighbor in GetNodeCollection(domainModel).Where(n => n.ParentId == node.ParentId && n.Order > node.Order && n.Id != nodeId))
                        {
                            neighbor.Order--;
                            changedNodes.Add(neighbor.Id);
                        }

                        foreach (var neighbor in GetNodeCollection(domainModel).Where(n => n.ParentId == parentId && n.Order >= order && n.Id != nodeId))
                        {
                            neighbor.Order++;
                            changedNodes.Add(neighbor.Id);
                        }
                    }
                }

                node.ParentId = parentId;
                node.Order = order;
                changedNodes.Add(nodeId);

                foreach (var id in changedNodes)
                {
                    _changeTracker.AddChange<Node>(id, EntityChangeType.Updated, domainModel);
                }

                _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return new
            {
                Root = GetRootNode()
            };
        }

        public object CreateNode(NodeSettingsModel node)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof (NodeSettingsModel));
            definition.ValidateInput(node, true, null);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var target = new Node
                {
                    Enabled = node.Enabled,
                    HandlerId = node.HandlerId,
                    ParentId = node.ParentId,
                    Order = node.Order,
                    Path = node.Path,
                    Settings = node.Settings != null ? JsonConvert.SerializeObject(node.Settings) : null,
                    Title = node.Title
                };

                GetNodeCollection(domainModel).Add(target);
                domainModel.SaveChanges();
                _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                _changeTracker.AddChange(target, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();
                domainModel.CompleteTransaction();

                node.NodeId = target.Id;

                var handler = (INodeHandler)DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(node.HandlerId).Item1);

                node.Customizable = handler.SettingsModel != null;

                if (node.Customizable)
                {
                    node.SettingsModelId = modelCache.GetDefinition(handler.SettingsModel).Id;
                }

                return node;
            }
        }

        public object DeleteNode(long id)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = GetNodeCollection(domainModel).First(n => n.Id == id);
                var parentId = node.ParentId;
                GetNodeCollection(domainModel).Remove(node);
                _changeTracker.AddChange(node, EntityChangeType.Removed, domainModel);
                domainModel.SaveChanges();

                var nodes = GetNodeCollection(domainModel).Where(p => p.ParentId == parentId).OrderBy(p => p.Order).ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Order != i)
                    {
                        nodes[i].Order = i;
                        _changeTracker.AddChange(nodes[i], EntityChangeType.Updated, domainModel);
                    }
                }

                _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return null;
        }

        public object UpdateNode(NodeSettingsModel node)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof(NodeSettingsModel));
            definition.ValidateInput(node, false, null);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var record = GetNodeCollection(domainModel).Single(p => p.Id == node.NodeId);

                record.Title = node.Title;
                record.ParentId = node.ParentId;
                record.Path = node.Path;
                record.Enabled = node.Enabled;

                _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                _changeTracker.AddChange(record, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return node;
        }

        public object ChangeSettings(long nodeId, object settings)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = GetNodeCollection(domainModel).Single(p => p.Id == nodeId);

                node.Settings = settings != null ? JsonConvert.SerializeObject(settings) : null;

                _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                _changeTracker.AddChange(node, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return settings;
        }

        protected override bool HasChildren
        {
            get { return true; }
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            var split = path.Split(new[] {'/'});
            long nodeId;
            if (split.Length != 2 || !long.TryParse(split[0], out nodeId) || split[1] != "access")
            {
                return null;
            }

            var handler = DependencyResolver.Resolve<NodeAccessNodeHandler>();

            handler.Initialize(SiteId, nodeId);

            ((INodeHandler) handler).Initialize(nodeId, null, null, path, AccessType);

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
