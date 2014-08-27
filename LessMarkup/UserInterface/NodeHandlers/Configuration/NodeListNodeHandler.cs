/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Configuration;

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

        public NodeListNodeHandler(IModuleIntegration moduleIntegration, IDataCache dataCache, ISiteMapper siteMapper)
        {
            _moduleIntegration = moduleIntegration;
            _dataCache = dataCache;
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

        protected override Dictionary<string, object> GetViewData()
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();

            var node = DependencyResolver.Resolve<NodeSettingsModel>();

            return new Dictionary<string, object>
            {
                { "Root", node.GetRootNode(SiteId) },
                { "NodeSettingsModelId", modelCache.GetDefinition(typeof(NodeSettingsModel)).Id },
                { "NodeHandlers", _moduleIntegration.GetNodeHandlers().Select(id => new { Id = id, Handler = _moduleIntegration.GetNodeHandler(id )}).Select(h => new
                {
                    h.Id,
                    Name = GetHandlerName(h.Handler.Item1, h.Handler.Item2),
                })}
            };
        }

        public object UpdateParent(long nodeId, long? parentId, int order)
        {
            var node = DependencyResolver.Resolve<NodeSettingsModel>();
            node.NodeId = nodeId;
            node.ParentId = parentId;
            node.Order = order;
            return node.UpdateParent(SiteId);
        }

        public object CreateNode(NodeSettingsModel node)
        {
            return node.CreateNode(SiteId);
        }

        public object DeleteNode(long id)
        {
            var node = DependencyResolver.Resolve<NodeSettingsModel>();
            node.NodeId = id;

            return node.DeleteNode(SiteId);
        }

        public object UpdateNode(NodeSettingsModel node)
        {
            return node.UpdateNode(SiteId);
        }

        public object ChangeSettings(long nodeId, object settings)
        {
            var node = DependencyResolver.Resolve<NodeSettingsModel>();
            node.Settings = settings;
            node.NodeId = nodeId;
            return node.ChangeSettings(SiteId);
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

            ((INodeHandler) handler).Initialize(nodeId, null, null, path, FullPath + "/" + path, AccessType);

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
