/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Configuration;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Configuration
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.NodeSettings)]
    public class NodeSettingsModel : IInputSource
    {
        private readonly IModuleIntegration _moduleIntegration;
        private readonly IModuleProvider _moduleProvider;
        private readonly List<NodeSettingsModel> _children = new List<NodeSettingsModel>();
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly IChangeTracker _changeTracker;

        public NodeSettingsModel(IModuleIntegration moduleIntegration, IModuleProvider moduleProvider, IDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker)
        {
            _moduleIntegration = moduleIntegration;
            _moduleProvider = moduleProvider;
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _changeTracker = changeTracker;
        }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Title, Required = true)]
        public string Title { get; set; }

        [InputField(InputFieldType.Select, UserInterfaceTextIds.Handler, Required = true)]
        public string HandlerId { get; set; }

        public object Settings { get; set; }

        public string SettingsModelId { get; set; }
        public long NodeId { get; set; }

        public bool Customizable { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.Enabled, DefaultValue = true)]
        public bool Enabled { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.AddToMenu, DefaultValue = false)]
        public bool AddToMenu { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Path, Required = true)]
        public string Path { get; set; }

        public int Order { get; set; }

        public string RoleText { get; set; }

        public List<NodeSettingsModel> Children { get { return _children; } }

        public long? ParentId { get; set; }

        public List<EnumSource> GetEnumValues(string fieldName)
        {
            switch (fieldName)
            {
                case "HandlerId":
                {
                    var modules = _moduleProvider.Modules.Select(m => m.ModuleType).ToList();
                    return
                        _moduleIntegration.GetNodeHandlers()
                            .Select(id => new {Id = id, Handler = _moduleIntegration.GetNodeHandler(id)})
                            .Where(h => modules.Contains(h.Handler.Item2))
                            .Select(h => new EnumSource
                            {
                                Value = h.Id,
                                Text = NodeListNodeHandler.GetHandlerName(h.Handler.Item1, h.Handler.Item2)
                            }).ToList();
                }
                default:
                    throw new ArgumentOutOfRangeException("fieldName");
            }
        }

        public object CreateNode(long siteId)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof(NodeSettingsModel));
            definition.ValidateInput(this, true, null);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var target = new Node
                {
                    Enabled = Enabled,
                    HandlerId = HandlerId,
                    ParentId = ParentId,
                    Order = Order,
                    Path = Path,
                    AddToMenu = AddToMenu,
                    Settings = Settings != null ? JsonConvert.SerializeObject(Settings) : null,
                    Title = Title
                };

                var collection = domainModel.GetSiteCollection<Node>(siteId);

                collection.Add(target);
                domainModel.SaveChanges();
                _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);
                _changeTracker.AddChange(target, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();
                domainModel.CompleteTransaction();

                NodeId = target.Id;

                var handler = (INodeHandler)DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(HandlerId).Item1);

                Customizable = handler.SettingsModel != null;

                if (Customizable)
                {
                    SettingsModelId = modelCache.GetDefinition(handler.SettingsModel).Id;
                }

                return this;
            }
        }

        public object UpdateNode(long siteId)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof(NodeSettingsModel));
            definition.ValidateInput(this, false, null);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var record = domainModel.GetSiteCollection<Node>(siteId).Single(p => p.Id == NodeId);

                record.Title = Title;
                record.ParentId = ParentId;
                record.Path = Path;
                record.Enabled = Enabled;
                record.AddToMenu = AddToMenu;

                _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);
                _changeTracker.AddChange(record, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return this;
        }

        public object DeleteNode(long siteId)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = domainModel.GetSiteCollection<Node>(siteId).First(n => n.Id == NodeId);
                var parentId = node.ParentId;
                domainModel.GetSiteCollection<Node>(siteId).Remove(node);
                _changeTracker.AddChange(node, EntityChangeType.Removed, domainModel);
                domainModel.SaveChanges();

                var nodes = domainModel.GetSiteCollection<Node>(siteId).Where(p => p.ParentId == parentId).OrderBy(p => p.Order).ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Order != i)
                    {
                        nodes[i].Order = i;
                        _changeTracker.AddChange(nodes[i], EntityChangeType.Updated, domainModel);
                    }
                }

                _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return null;
        }

        private void NormalizeTree(List<NodeSettingsModel> nodes, NodeSettingsModel rootNode, IDomainModel domainModel, HashSet<long> changedNodes, long siteId)
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
                    var record = domainModel.GetSiteCollection<Node>(siteId).First(n => n.Id == node.NodeId);
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
                        var record = domainModel.GetSiteCollection<Node>(siteId).First(n => n.Id == child.NodeId);
                        record.Order = i;
                        changedNodes.Add(record.Id);
                    }
                }
            }
        }

        public NodeSettingsModel GetRootNode(long siteId)
        {
            NodeSettingsModel rootNode;
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var nodes = new List<NodeSettingsModel>();
            var nodeIds = new Dictionary<long, NodeSettingsModel>();

            using (var domainModel = _domainModelProvider.Create())
            {
                var collection = domainModel.GetSiteCollection<Node>(siteId);

                foreach (var source in collection.Select(n => new
                {
                    n.Enabled,
                    n.HandlerId,
                    n.ParentId,
                    n.Id,
                    n.Path,
                    n.Settings,
                    n.Title,
                    n.Order,
                    n.AddToMenu
                }).OrderBy(n => n.Order))
                {
                    var node = DependencyResolver.Resolve<NodeSettingsModel>();

                    var handler = source.HandlerId != null ? (INodeHandler)DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(source.HandlerId).Item1) : null;

                    node.ParentId = source.ParentId;
                    node.Enabled = source.Enabled;
                    node.HandlerId = source.HandlerId;
                    node.NodeId = source.Id;
                    node.AddToMenu = source.AddToMenu;
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
                        var record = collection.First(n => n.Id == node.NodeId);
                        record.ParentId = null;
                        changedNodes.Add(node.NodeId);
                    }
                    else
                    {
                        parent.Children.Add(node);
                    }
                }

                rootNode = nodes.FirstOrDefault(n => !n.ParentId.HasValue);

                NormalizeTree(nodes, rootNode, domainModel, changedNodes, siteId);

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


        public object ChangeSettings(long siteId)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = domainModel.GetSiteCollection<Node>(siteId).Single(p => p.Id == NodeId);

                node.Settings = Settings != null ? JsonConvert.SerializeObject(Settings) : null;

                _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);
                _changeTracker.AddChange(node, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return Settings;
        }

        public object UpdateParent(long siteId)
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var collection = domainModel.GetSiteCollection<Node>(siteId);

                var node = collection.First(n => n.Id == NodeId);

                var changedNodes = new HashSet<long>();

                if (node.ParentId.HasValue != ParentId.HasValue) // we exchange root node and specified node
                {
                    Node newRootNode;
                    Node oldRootNode;

                    if (node.ParentId.HasValue)
                    {
                        newRootNode = node;
                        oldRootNode = collection.First(n => !n.ParentId.HasValue);
                    }
                    else
                    {
                        oldRootNode = node;
                        newRootNode = collection.First(n => n.Id == ParentId.Value);
                    }

                    if (!newRootNode.ParentId.HasValue)
                    {
                        throw new Exception(LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.CannotHaveTwoRootNodes));
                    }

                    foreach (var neighbor in collection.Where(n => n.ParentId == newRootNode.ParentId.Value && n.Order > newRootNode.Order))
                    {
                        neighbor.Order--;
                        changedNodes.Add(neighbor.Id);
                    }

                    newRootNode.Order = 0;
                    newRootNode.ParentId = null;
                    changedNodes.Add(newRootNode.Id);

                    foreach (var neighbor in collection.Where(n => n.ParentId == newRootNode.Id && n.Order >= 0 && n.Id != NodeId))
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
                    if (node.ParentId == ParentId)
                    {
                        if (Order > node.Order)
                        {
                            foreach (var neighbor in collection.Where(n => n.ParentId == ParentId && n.Id != NodeId && n.Order > node.Order && n.Order <= Order))
                            {
                                neighbor.Order--;
                                changedNodes.Add(neighbor.Id);
                            }
                        }
                        else if (Order < node.Order)
                        {
                            foreach (var neighbor in collection.Where(n => n.ParentId == ParentId && n.Id != NodeId && n.Order >= Order && n.Order < node.Order))
                            {
                                neighbor.Order++;
                                changedNodes.Add(neighbor.Id);
                            }
                        }
                    }
                    else
                    {
                        foreach (var neighbor in collection.Where(n => n.ParentId == node.ParentId && n.Order > node.Order && n.Id != NodeId))
                        {
                            neighbor.Order--;
                            changedNodes.Add(neighbor.Id);
                        }

                        foreach (var neighbor in collection.Where(n => n.ParentId == ParentId && n.Order >= Order && n.Id != NodeId))
                        {
                            neighbor.Order++;
                            changedNodes.Add(neighbor.Id);
                        }
                    }
                }

                node.ParentId = ParentId;
                node.Order = Order;
                changedNodes.Add(NodeId);

                foreach (var id in changedNodes)
                {
                    _changeTracker.AddChange<Node>(id, EntityChangeType.Updated, domainModel);
                }

                _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }

            return new
            {
                Root = GetRootNode(siteId)
            };
        }
    }
}
