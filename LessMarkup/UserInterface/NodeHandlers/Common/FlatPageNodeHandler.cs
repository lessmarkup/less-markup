/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Common;
using LessMarkup.UserInterface.Model.Structure;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public class FlatPageNodeHandler : AbstractNodeHandler
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        class FlatNodeEntry
        {
            public string Title { get; set; }
            public object ViewData { get; set; }
            public string ViewBody { get; set; }
            public Type HandlerType { get; set; }
            public long NodeId { get; set; }
            public string Settings { get; set; }
            public string Anchor { get; set; }
            public string UniqueId { get; set; }
            public int Level { get; set; }
            public string Path { get; set; }
            public string FullPath { get; set; }
            public NodeAccessType AccessType { get; set; }
            public ICachedNodeInformation Source { get; set; }
        }

        class TreeNodeEntry
        {
            public string Anchor { get; set; }
            public string Title { get; set; }

            public List<TreeNodeEntry> Children { get; set; }
        }

        private readonly List<FlatNodeEntry> _flatNodeList = new List<FlatNodeEntry>();
        private TreeNodeEntry _treeRoot;
        private readonly List<string> _scripts = new List<string>(); 

        public FlatPageNodeHandler(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        private void FillFlatAndTreeList(ICachedNodeInformation parent, List<FlatNodeEntry> nodes, TreeNodeEntry parentTreeNode, string anchor = "", int level = 1, int maxLevel = 2)
        {
            if (parent.Children == null)
            {
                return;
            }

            foreach (var child in parent.Children)
            {
                if (child.HandlerType == null)
                {
                    continue;
                }

                if (child.HandlerType == typeof (FlatPageNodeHandler) || !child.Visible)
                {
                    continue;
                }

                var accessType = child.CheckRights(_currentUser);

                if (accessType == NodeAccessType.NoAccess)
                {
                    continue;
                }

                var childAnchor = string.IsNullOrEmpty(anchor) ? "" : anchor + "_";

                var entry = new FlatNodeEntry
                {
                    Title = child.Title,
                    HandlerType = child.HandlerType,
                    NodeId = child.NodeId,
                    Settings = child.Settings,
                    Anchor = childAnchor + child.Path,
                    Level = level,
                    Path = child.FullPath,
                    FullPath = child.FullPath,
                    Source = child,
                    AccessType = accessType
                };

                var treeNode = new TreeNodeEntry
                {
                    Title = entry.Title,
                    Anchor = entry.Anchor,
                    Children = new List<TreeNodeEntry>()
                };

                parentTreeNode.Children.Add(treeNode);

                nodes.Add(entry);

                if (level < maxLevel)
                {
                    FillFlatAndTreeList(child, nodes, treeNode, entry.Anchor, level + 1, maxLevel);
                }
            }
        }

        protected override object Initialize(object controller)
        {
            var settingsModel = GetSettings<FlatPageSettingsModel>();

            var nodeCache = _dataCache.Get<INodeCache>();

            _treeRoot = new TreeNodeEntry { Children = new List<TreeNodeEntry>() };

            if (ObjectId.HasValue)
            {
                var currentNode = nodeCache.GetNode(ObjectId.Value);
                FillFlatAndTreeList(currentNode, _flatNodeList, _treeRoot);
            }

            if (settingsModel == null || settingsModel.LoadOnShow)
            {
                foreach (var node in _flatNodeList)
                {
                    var handler = CreateChildHandler(node.HandlerType);
                    object nodeSettings = null;

                    if (handler.SettingsModel != null && !string.IsNullOrEmpty(node.Settings))
                    {
                        nodeSettings = JsonConvert.DeserializeObject(node.Settings, handler.SettingsModel);
                    }

                    handler.Initialize(node.NodeId, nodeSettings, controller, node.Path, node.FullPath, node.AccessType);

                    node.ViewData = handler.GetViewData();
                    node.ViewBody = LoadNodeViewModel.GetViewTemplate(handler, _dataCache, (System.Web.Mvc.Controller) controller);

                    var scripts = handler.Scripts;

                    if (scripts != null)
                    {
                        _scripts.AddRange(scripts);
                    }
                }

                _flatNodeList.RemoveAll(n => n.ViewBody == null);
            }

            var pageIndex = 1;

            foreach (var node in _flatNodeList)
            {
                node.UniqueId = string.Format("flatpage{0}", pageIndex++);
            }

            return null;
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var settingsModel = GetSettings<FlatPageSettingsModel>();

            return new Dictionary<string, object>
            {
                { "Tree", _treeRoot.Children },
                { "Flat", _flatNodeList.Select(f => new
                {
                    f.Anchor, 
                    //f.HandlerType, 
                    f.Level, 
                    f.NodeId, 
                    f.Path, 
                    //f.Settings, 
                    f.Title, 
                    f.UniqueId, 
                    f.ViewBody, 
                    f.ViewData
                }).ToList() },
                { "Position", settingsModel != null ? settingsModel.Position : FlatPagePosition.Right },
                { "Scripts", _scripts }
            };
        }

        protected override Type SettingsModel
        {
            get { return typeof(FlatPageSettingsModel); }
        }

        protected override bool HasChildren
        {
            get { return true; }
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            var node = _flatNodeList.FirstOrDefault(n => n.Path == path);

            if (node == null)
            {
                return null;
            }

            var accessType = node.Source.CheckRights(_currentUser);

            if (accessType == NodeAccessType.NoAccess)
            {
                return null;
            }

            var handler = (INodeHandler) Interfaces.DependencyResolver.Resolve(node.HandlerType);

            object nodeSettings = null;

            if (handler.SettingsModel != null && !string.IsNullOrEmpty(node.Settings))
            {
                nodeSettings = JsonConvert.DeserializeObject(node.Settings, handler.SettingsModel);
            }

            handler.Initialize(node.NodeId, nodeSettings, null, node.Path, node.FullPath, accessType);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Id = node.NodeId,
                Path = path,
                Title = node.Title
            };
        }
    }
}
