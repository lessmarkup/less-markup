/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Engine.Configuration;
using LessMarkup.Engine.Language;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.NodeHandlers;
using LessMarkup.UserInterface.NodeHandlers.Configuration;
using LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration;
using LessMarkup.UserInterface.NodeHandlers.User;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NodeCache : INodeCache
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDataCache _dataCache;
        private long? _siteId;

        private readonly List<ICachedNodeInformation> _cachedNodes = new List<ICachedNodeInformation>();
        private readonly Dictionary<long, ICachedNodeInformation> _idToNode = new Dictionary<long, ICachedNodeInformation>();
        private CachedNodeInformation _rootNode;

        public ICachedNodeInformation RootNode { get { return _rootNode; } }

        public NodeCache(IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration, IEngineConfiguration engineConfiguration, IDataCache dataCache)
        {
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
            _engineConfiguration = engineConfiguration;
            _dataCache = dataCache;
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
                foreach (var cachedNodeInformation in node.Children.Where(c => c.Enabled))
                {
                    var child = (CachedNodeInformation) cachedNodeInformation;
                    child.Parent = node;
                    InitializeTree(child);
                }
            }
        }

        private void InitializeNode(CachedNodeInformation node, List<CachedNodeInformation> nodes, int from, int count)
        {
            if (count == 0)
            {
                return;
            }

            var lowLevel = nodes[from].Level;
            var to = from + count;

            var firstNode = nodes[from];
            node.AddChild(firstNode);

            from++;

            for (int i = from; i < to;)
            {
                var nextNode = nodes[i];
                if (nextNode.Level <= lowLevel)
                {
                    node.AddChild(nextNode);
                    i++;
                    continue;
                }
                var parent = (CachedNodeInformation) node.Children.Last();
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

        public ICachedNodeInformation GetNode(long nodeId)
        {
            ICachedNodeInformation ret;
            return _idToNode.TryGetValue(nodeId, out ret) ? ret : null;
        }

        public void GetNode(string path, out ICachedNodeInformation node, out string rest)
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

        private CachedNodeInformation AddVirtualNode<T>(string path, string title, string moduleType, NodeAccessType accessType) where T : INodeHandler
        {
            var nodeId = _idToNode.Keys.Max() + 1;

            var node = new CachedNodeInformation
            {
                AccessList = new List<CachedNodeAccess>
                {
                    new CachedNodeAccess {AccessType = accessType },
                },
                FullPath = path.ToLower(),
                Path = path.ToLower(),
                HandlerModuleType = moduleType,
                ParentNodeId = _rootNode.NodeId,
                Parent = _rootNode,
                Title = title,
                HandlerType = typeof(T),
                NodeId = nodeId,
                HandlerId = path,
                Root = _rootNode,
                Visible = false,
            };

            _rootNode.AddChild(node);
            _idToNode[nodeId] = node;

            return node;
        }

        public void Initialize(long? siteId, out DateTime? expirationTime, long? objectId = null)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            _siteId = siteId;

            expirationTime = null;

            List<CachedNodeInformation> cachedNodes = null;

            if (_siteId.HasValue)
            { 
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
                        Visible = true,
                        AccessList = p.NodeAccess.Select(a => new CachedNodeAccess
                        {
                            AccessType = a.AccessType,
                            GroupId = a.GroupId,
                            UserId = a.UserId
                        }).ToList()
                    }).ToList();
                }
            }

            if (cachedNodes == null || cachedNodes.Count == 0)
            {
                if (cachedNodes == null)
                {
                    cachedNodes = new List<CachedNodeInformation>();
                }

                cachedNodes.Add(new CachedNodeInformation
                {
                    AccessList = new List<CachedNodeAccess> {new CachedNodeAccess {AccessType = NodeAccessType.Read}},
                    HandlerModuleType = Constants.ModuleType.MainModule,
                    HandlerType = typeof (DefaultRootNodeHandler),
                    Title = "Home",
                    NodeId = 1,
                    HandlerId = "home",
                    Visible = true
                });
            }

            _rootNode = cachedNodes[0];

            InitializeNode(_rootNode, cachedNodes, 1, cachedNodes.Count-1);

            InitializeTree(_rootNode);

            _rootNode.Root = _rootNode;

            AddVirtualNode<ConfigurationRootNodeHandler>(Constants.NodePath.Configuration,
                LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.Configuration),
                Constants.ModuleType.UserInterface, NodeAccessType.NoAccess);

            string adminLoginPage;

            if (_siteId.HasValue)
            {
                var siteConfiguration = _dataCache.Get<SiteConfigurationCache>();
                adminLoginPage = siteConfiguration.AdminLoginPage;
                if (string.IsNullOrWhiteSpace(adminLoginPage))
                {
                    adminLoginPage = _engineConfiguration.AdminLoginPage;
                }
            }
            else
            {
                adminLoginPage = _engineConfiguration.AdminLoginPage;
            }

            if (!string.IsNullOrWhiteSpace(adminLoginPage))
            {
                AddVirtualNode<AdministratorLoginNodeHandler>(adminLoginPage,
                    LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.AdministratorLogin),
                    Constants.ModuleType.UserInterface, NodeAccessType.Read);
            }

            if (_siteId.HasValue)
            {
                var node = AddVirtualNode<UserProfileNodeHandler>(Constants.NodePath.Profile,
                    LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.UserProfile),
                    Constants.ModuleType.UserInterface, NodeAccessType.Read);
                node.LoggedIn = true;
            }

            _cachedNodes.Clear();
        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.Node || (entityType == EntityType.Site && _siteId.HasValue && entityId == _siteId.Value);
        }

        private static readonly EntityType[] _handledTypes = {EntityType.Node, EntityType.Site};

        public EntityType[] HandledTypes { get { return _handledTypes; } }
    }
}
