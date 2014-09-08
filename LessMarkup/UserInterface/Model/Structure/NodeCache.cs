﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.NodeHandlers;
using LessMarkup.UserInterface.NodeHandlers.Configuration;
using LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration;
using LessMarkup.UserInterface.NodeHandlers.User;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NodeCache : AbstractCacheHandler, INodeCache
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private long? _siteId;

        private readonly List<ICachedNodeInformation> _cachedNodes = new List<ICachedNodeInformation>();
        private readonly Dictionary<long, ICachedNodeInformation> _idToNode = new Dictionary<long, ICachedNodeInformation>();
        private CachedNodeInformation _rootNode;

        public ICachedNodeInformation RootNode { get { return _rootNode; } }
        public IReadOnlyList<ICachedNodeInformation> Nodes { get { return _cachedNodes; } }

        public NodeCache(IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration, IEngineConfiguration engineConfiguration, IDataCache dataCache, ICurrentUser currentUser)
            : base(new[] { typeof(Node), typeof(Site) })
        {
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
            _engineConfiguration = engineConfiguration;
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        private void InitializeTree(CachedNodeInformation node, List<CachedNodeInformation> allNodes)
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
                node.FullPath = "/";
            }
            else
            {
                node.Path = node.Path.Trim().ToLower();

                if (string.IsNullOrEmpty(node.Path))
                {
                    return;
                }

                node.FullPath = node.Parent == RootNode ? ("/" + node.Path) : (node.Parent.FullPath + "/" + node.Path);
            }

            _cachedNodes.Add(node);
            _idToNode[node.NodeId] = node;

            foreach (var child in allNodes.Where(n => n.ParentNodeId == node.NodeId && n.Enabled))
            {
                node.AddChild(child);
                child.Parent = node;
                InitializeTree(child, allNodes);
            }
        }

        public ICachedNodeInformation GetNode(long nodeId)
        {
            ICachedNodeInformation ret;
            return _idToNode.TryGetValue(nodeId, out ret) ? ret : null;
        }

        private bool TraverseParents(ICachedNodeInformation node, Func<INodeHandler, string, string, string, long?, bool> preprocessFunc)
        {
            if (node.Parent != null)
            {
                if (TraverseParents(node.Parent, preprocessFunc))
                {
                    return true;
                }
            }

            return preprocessFunc(null, node.Title, node.FullPath, null, node.NodeId);
        }

        public INodeHandler GetNodeHandler(string path, object controller = null, Func<INodeHandler, string, string, string, long?, bool> preprocessFunc = null)
        {
            path = HttpUtility.UrlDecode(path);

            if (path != null)
            {
                var queryPost = path.IndexOf('?');
                if (queryPost >= 0)
                {
                    path = path.Substring(0, queryPost);
                }
            }

            ICachedNodeInformation node;
            string rest;

            GetNode(path, out node, out rest);

            if (node == null)
            {
                return null;
            }

            if (node.LoggedIn && !_currentUser.UserId.HasValue)
            {
                return null;
            }

            var accessType = node.CheckRights(_currentUser);

            if (accessType == NodeAccessType.NoAccess)
            {
                return null;
            }

            if (preprocessFunc != null && node.Parent != null)
            {
                TraverseParents(node.Parent, preprocessFunc);
            }

            var nodeHandler = (INodeHandler)DependencyResolver.Resolve(node.HandlerType);

            if (nodeHandler == null)
            {
                return null;
            }

            var currentTitle = node.Title;
            var currentPath = node.FullPath;

            var settings = node.Settings;

            object settingsObject = null;

            if (!string.IsNullOrWhiteSpace(settings) && nodeHandler.SettingsModel != null)
            {
                settingsObject = JsonConvert.DeserializeObject(settings, nodeHandler.SettingsModel);
            }

            nodeHandler.Initialize(node.NodeId, settingsObject, controller, node.Path, node.FullPath, accessType);

            bool first = true;

            while (!string.IsNullOrWhiteSpace(rest))
            {
                if (preprocessFunc != null && preprocessFunc(nodeHandler, currentTitle, currentPath, rest, first ? node.NodeId : (long?)null))
                {
                    return null;
                }

                first = false;

                var childSettings = nodeHandler.GetChildHandler(rest);
                if (childSettings == null)
                {
                    return null;
                }

                nodeHandler = childSettings.Handler;

                currentTitle = childSettings.Title;
                currentPath += "/" + childSettings.Path;

                if (string.IsNullOrWhiteSpace(childSettings.Rest))
                {
                    break;
                }

                rest = childSettings.Rest;
            }

            if (preprocessFunc != null && preprocessFunc(nodeHandler, currentTitle, currentPath, rest, first ? node.NodeId : (long?) null))
            {
                return null;
            }

            return nodeHandler;
        }

        public void GetNode(string path, out ICachedNodeInformation node, out string rest)
        {
            var nodeParts = (path ?? "").Split(new[] {'/'}).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

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
                FullPath = "/" + path.ToLower(),
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

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            _siteId = siteId;

            var cachedNodes = new List<CachedNodeInformation>();

            if (_siteId.HasValue)
            { 
                using (var domainModel = _domainModelProvider.Create())
                {
                    cachedNodes.AddRange(domainModel.GetSiteCollection<Node>().OrderBy(p => p.Order).Select(p => new CachedNodeInformation
                    {
                        NodeId = p.Id,
                        Enabled = p.Enabled,
                        HandlerId = p.HandlerId,
                        ParentNodeId = p.ParentId,
                        Order = p.Order,
                        Path = p.Path,
                        Title = p.Title,
                        Description = p.Description,
                        Settings = p.Settings,
                        Visible = true,
                        AddToMenu = p.AddToMenu,
                        AccessList = p.NodeAccess.Select(a => new CachedNodeAccess
                        {
                            AccessType = a.AccessType,
                            GroupId = a.GroupId,
                            UserId = a.UserId
                        }).ToList()
                    }).ToList());
                }
            }

            _rootNode = cachedNodes.FirstOrDefault(n => !n.ParentNodeId.HasValue);

            if (_rootNode == null)
            {
                _rootNode = new CachedNodeInformation
                {
                    AccessList = new List<CachedNodeAccess> {new CachedNodeAccess {AccessType = NodeAccessType.Read}},
                    HandlerModuleType = Constants.ModuleType.MainModule,
                    HandlerType = typeof (DefaultRootNodeHandler),
                    Title = "Home",
                    NodeId = 1,
                    HandlerId = "home",
                    Visible = true
                };
                cachedNodes.Add(_rootNode);
            }

            InitializeTree(_rootNode, cachedNodes);

            _rootNode.Root = _rootNode;

            AddVirtualNode<ConfigurationRootNodeHandler>(Constants.NodePath.Configuration,
                LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.Configuration),
                Constants.ModuleType.UserInterface, NodeAccessType.NoAccess);

            string adminLoginPage;

            var siteConfiguration = _dataCache.Get<ISiteConfiguration>();

            if (_siteId.HasValue)
            {
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

                if (siteConfiguration.HasUsers)
                {
                    AddVirtualNode<UserCardsNodeHandler>(Constants.NodePath.UserCards,
                        LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.UserCards),
                        Constants.ModuleType.UserInterface, NodeAccessType.Read);
                    AddVirtualNode<ForgotPasswordPageHandler>(Constants.NodePath.ForgotPassword,
                        LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.ForgotPassword),
                        Constants.ModuleType.UserInterface, NodeAccessType.Read);
                }
            }
        }
    }
}
