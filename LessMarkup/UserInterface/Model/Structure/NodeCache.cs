/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Engine.Helpers;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.UserInterface.NodeHandlers;
using LessMarkup.UserInterface.NodeHandlers.Configuration;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NodeCache : ICacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;

        private readonly List<CachedNodeInformation> _cachedNodes = new List<CachedNodeInformation>();
        private readonly Dictionary<long, CachedNodeInformation> _idToNode = new Dictionary<long, CachedNodeInformation>();
        private CachedNodeInformation _rootNode;

        public CachedNodeInformation RootNode { get { return _rootNode; } }

        public NodeCache(IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration)
        {
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
        }

        private void InitializeTree(CachedNodeInformation node)
        {
            if (!string.IsNullOrWhiteSpace(node.HandlerId))
            {
                var handler = _moduleIntegration.GetNodeHandler(node.HandlerId);

                if (handler != null)
                {
                    node.HandlerType = handler.Item1;
                    node.HandlerModuleType = handler.Item2;
                }
            }

            node.Root = _rootNode;

            if (node.Parent == null)
            {
                // root
                node.Path = "";
                node.FullPath = "";
            }
            else
            {
                node.Path = node.Path.Trim().ToLower();

                if (string.IsNullOrEmpty(node.Path))
                {
                    return;
                }

                node.FullPath = string.IsNullOrEmpty(node.Parent.FullPath) ? node.Path : node.Parent.FullPath + "/" + node.Path;
            }

            _cachedNodes.Add(node);
            _idToNode[node.NodeId] = node;

            if (node.Children != null)
            {
                foreach (var child in node.Children.Where(c => c.Enabled))
                {
                    child.Parent = node;
                    InitializeTree(child);
                }
            }
        }

        private void InitializeNode(CachedNodeInformation node, List<CachedNodeInformation> nodes, int from, int count)
        {
            node.Children = new List<CachedNodeInformation>();

            if (count == 0)
            {
                return;
            }

            var lowLevel = nodes[from].Level;
            var to = from + count;

            var firstNode = nodes[from];
            node.Children.Add(firstNode);

            from++;

            for (int i = from; i < to;)
            {
                var nextNode = nodes[i];
                if (nextNode.Level <= lowLevel)
                {
                    node.Children.Add(nextNode);
                    i++;
                    continue;
                }
                var parent = node.Children.Last();
                var childFrom = i;
                for (i++; i < to; i++)
                {
                    if (nodes[i].Level <= lowLevel)
                    {
                        break;
                    }
                }

                InitializeNode(parent, nodes, childFrom, i - childFrom);
            }
        }

        public CachedNodeInformation GetNode(long nodeId)
        {
            CachedNodeInformation ret;
            return _idToNode.TryGetValue(nodeId, out ret) ? ret : null;
        }

        public void GetNode(string path, out CachedNodeInformation node, out string rest)
        {
            var nodeParts = (path ?? "").ToLower().Split(new[] {'/'}).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            if (nodeParts.Count == 0)
            {
                node = _rootNode;
                rest = "";
                return;
            }

            node = _rootNode;

            while (nodeParts.Count > 0)
            {
                var pathPart = nodeParts[0].Trim();
                if (string.IsNullOrEmpty(pathPart))
                {
                    nodeParts.RemoveAt(0);
                    continue;
                }

                var child = node.Children == null ? null : node.Children.FirstOrDefault(c => c.Path == pathPart);

                if (child == null)
                {
                    break;
                }

                node = child;

                nodeParts.RemoveAt(0);
            }

            rest = nodeParts.Count > 0 ? string.Join("/", nodeParts) : "";
        }

        public void Initialize(out DateTime? expirationTime, long? objectId = null)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            expirationTime = null;

            List<CachedNodeInformation> cachedNodes;

            using (var domainModel = _domainModelProvider.Create())
            {
                cachedNodes = domainModel.GetSiteCollection<Node>().OrderBy(p => p.Order).Select(p => new CachedNodeInformation
                {
                    NodeId = p.NodeId,
                    Enabled = p.Enabled,
                    HandlerId = p.HandlerId,
                    Level = p.Level,
                    Order = p.Order,
                    Path = p.Path,
                    Title = p.Title,
                    Settings = p.Settings,
                    AccessList = p.NodeAccess.Select(a => new CachedNodeAccess
                    {
                        AccessType = a.AccessType,
                        GroupId = a.GroupId,
                        UserId = a.UserId
                    }).ToList()
                }).ToList();
            }

            if (cachedNodes.Count == 0)
            {
                cachedNodes.Add(new CachedNodeInformation
                {
                    AccessList = new List<CachedNodeAccess> {new CachedNodeAccess {AccessType = NodeAccessType.Read}},
                    HandlerModuleType = ModuleType.MainModule,
                    HandlerType = typeof (DefaultRootNodeHandler),
                    Title = "Home",
                    NodeId = 1,
                    HandlerId = "home",
                    Children = new List<CachedNodeInformation>(),
                });
            }

            _rootNode = cachedNodes[0];

            InitializeNode(_rootNode, cachedNodes, 1, cachedNodes.Count-1);

            InitializeTree(_rootNode);

            _rootNode.Root = _rootNode;

            var nodeId = _idToNode.Keys.Max() + 1;

            var configurationNode = new CachedNodeInformation
            {
                AccessList = new List<CachedNodeAccess>
                {
                    new CachedNodeAccess {AccessType = NodeAccessType.NoAccess},
                },
                FullPath = "configuration",
                Path = "configuration",
                HandlerModuleType = ModuleType.MainModule,
                ParentNodeId = _rootNode.NodeId,
                Parent = _rootNode,
                Title = LanguageHelper.GetText(ModuleType.MainModule, MainModuleTextIds.Configuration),
                HandlerType = typeof (ConfigurationRootNodeHandler),
                NodeId = nodeId,
                HandlerId = "configuration",
                Root = _rootNode,
                Children = new List<CachedNodeInformation>()
            };

            _rootNode.Children.Add(configurationNode);
            _idToNode[nodeId] = configurationNode;

            _cachedNodes.Clear();

        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.Node;
        }

        private static readonly EntityType[] _handledTypes = {EntityType.Node};

        public EntityType[] HandledTypes { get { return _handledTypes; } }
    }
}
