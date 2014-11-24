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
        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly IChangeTracker _changeTracker;

        public NodeSettingsModel(IModuleIntegration moduleIntegration, IModuleProvider moduleProvider, ILightDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker)
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
            switch (fieldName.FromJsonCase())
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

        public object CreateNode()
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof(NodeSettingsModel));
            definition.ValidateInput(this, true, null);

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

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                domainModel.Create(target);
                _changeTracker.AddChange(target, EntityChangeType.Added, domainModel);
                domainModel.CompleteTransaction();
            }

            NodeId = target.Id;

            var handler = (INodeHandler)DependencyResolver.Resolve(_moduleIntegration.GetNodeHandler(HandlerId).Item1);

            Customizable = handler.SettingsModel != null;

            if (Customizable)
            {
                SettingsModelId = modelCache.GetDefinition(handler.SettingsModel).Id;
            }

            return this;
        }

        public object UpdateNode()
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof(NodeSettingsModel));
            definition.ValidateInput(this, false, null);

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var record = domainModel.Query().Find<Node>(NodeId);

                record.Title = Title;
                record.ParentId = ParentId;
                record.Path = Path;
                record.Enabled = Enabled;
                record.AddToMenu = AddToMenu;

                domainModel.Update(record);
                _changeTracker.AddChange(record, EntityChangeType.Updated, domainModel);

                domainModel.CompleteTransaction();
            }

            return this;
        }

        public object DeleteNode()
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = domainModel.Query().Find<Node>(NodeId);
                var parentId = node.ParentId;
                domainModel.Delete<Node>(node.Id);
                _changeTracker.AddChange(node, EntityChangeType.Removed, domainModel);

                var nodes = domainModel.Query().From<Node>().Where("ParentId = $", parentId).ToList<Node>();

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Order != i)
                    {
                        nodes[i].Order = i;
                        domainModel.Update(nodes[i]);
                        _changeTracker.AddChange(nodes[i], EntityChangeType.Updated, domainModel);
                    }
                }

                domainModel.CompleteTransaction();
            }

            return null;
        }

        private void NormalizeTree(List<NodeSettingsModel> nodes, NodeSettingsModel rootNode, ILightDomainModel domainModel, HashSet<long> changedNodes)
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
                    var record = domainModel.Query().Find<Node>(node.NodeId);
                    if (record.ParentId != node.ParentId)
                    {
                        record.ParentId = node.ParentId;
                        domainModel.Update(record);
                    }

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
                        var record = domainModel.Query().Find<Node>(child.NodeId);
                        if (record.Order != i)
                        {
                            record.Order = i;
                            domainModel.Update(record);
                        }

                        changedNodes.Add(record.Id);
                    }
                }
            }
        }

        public NodeSettingsModel GetRootNode()
        {
            NodeSettingsModel rootNode;
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var nodes = new List<NodeSettingsModel>();
            var nodeIds = new Dictionary<long, NodeSettingsModel>();

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var source in domainModel.Query().From<Node>().ToList<Node>().Select(n => new
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
                        var record = domainModel.Query().Find<Node>(node.NodeId);
                        record.ParentId = null;
                        domainModel.Update(record);
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
                }
            }

            return rootNode;
        }


        public object ChangeSettings()
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = domainModel.Query().Find<Node>(NodeId);

                node.Settings = Settings != null ? JsonConvert.SerializeObject(Settings) : null;

                domainModel.Update(node);
                _changeTracker.AddChange(node, EntityChangeType.Updated, domainModel);

                domainModel.CompleteTransaction();
            }

            return Settings;
        }

        public object UpdateParent()
        {
            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var node = domainModel.Query().Find<Node>(NodeId);

                var changedNodes = new HashSet<long>();

                if (node.ParentId.HasValue != ParentId.HasValue) // we exchange root node and specified node
                {
                    Node newRootNode;
                    Node oldRootNode;

                    if (node.ParentId.HasValue)
                    {
                        newRootNode = node;
                        oldRootNode = domainModel.Query().From<Node>().Where("ParentId IS NULL").First<Node>();
                    }
                    else
                    {
                        oldRootNode = node;
                        newRootNode = domainModel.Query().Find<Node>(ParentId.Value);
                    }

                    if (!newRootNode.ParentId.HasValue)
                    {
                        throw new Exception(LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.CannotHaveTwoRootNodes));
                    }

                    foreach (var neighbor in domainModel.Query().From<Node>().Where("ParentId = $ AND Order > $", newRootNode.ParentId.Value, newRootNode.Order).ToList<Node>())
                    {
                        neighbor.Order--;
                        domainModel.Update(neighbor);
                        changedNodes.Add(neighbor.Id);
                    }

                    newRootNode.Order = 0;
                    newRootNode.ParentId = null;
                    changedNodes.Add(newRootNode.Id);

                    foreach (var neighbor in domainModel.Query().From<Node>().Where("ParentId = $ AND Id != $ AND Order > 0", newRootNode.Id, NodeId).ToList<Node>())
                    {
                        neighbor.Order++;
                        domainModel.Update(neighbor);
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
                            foreach (var neighbor in domainModel.Query().From<Node>().Where("ParentId = $ AND Id != $ AND Order > $ AND Order <= $", ParentId, NodeId, node.Order, Order).ToList<Node>())
                            {
                                neighbor.Order--;
                                domainModel.Update(neighbor);
                                changedNodes.Add(neighbor.Id);
                            }
                        }
                        else if (Order < node.Order)
                        {
                            foreach (var neighbor in domainModel.Query().From<Node>().Where("ParentId = $ AND Id != $ AND Order >= $ AND Order < $", ParentId, NodeId, Order, node.Order).ToList<Node>())
                            {
                                neighbor.Order++;
                                domainModel.Update(neighbor);
                                changedNodes.Add(neighbor.Id);
                            }
                        }
                    }
                    else
                    {
                        foreach (var neighbor in domainModel.Query().From<Node>().Where("ParentId = $ AND Order > $ AND Id != $", node.ParentId, node.Order, NodeId).ToList<Node>())
                        {
                            neighbor.Order--;
                            domainModel.Update(neighbor);
                            changedNodes.Add(neighbor.Id);
                        }

                        foreach (var neighbor in domainModel.Query().From<Node>().Where("ParentId = $ AND Order >= $ AND Id != $", ParentId, Order, NodeId).ToList<Node>())
                        {
                            neighbor.Order++;
                            domainModel.Update(neighbor);
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

                domainModel.CompleteTransaction();
            }

            return new
            {
                Root = GetRootNode()
            };
        }
    }
}
