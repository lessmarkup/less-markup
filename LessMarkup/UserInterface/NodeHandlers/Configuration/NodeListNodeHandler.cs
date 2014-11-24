/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
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

        public NodeListNodeHandler(IModuleIntegration moduleIntegration, IDataCache dataCache)
        {
            _moduleIntegration = moduleIntegration;
            _dataCache = dataCache;
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
                { "Root", node.GetRootNode() },
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
            return node.UpdateParent();
        }

        public object CreateNode(NodeSettingsModel node)
        {
            return node.CreateNode();
        }

        public object DeleteNode(long id)
        {
            var node = DependencyResolver.Resolve<NodeSettingsModel>();
            node.NodeId = id;

            return node.DeleteNode();
        }

        public object UpdateNode(NodeSettingsModel node)
        {
            return node.UpdateNode();
        }

        public object ChangeSettings(long nodeId, object settings)
        {
            var node = DependencyResolver.Resolve<NodeSettingsModel>();
            node.Settings = settings;
            node.NodeId = nodeId;
            return node.ChangeSettings();
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

            handler.Initialize(nodeId);

            ((INodeHandler) handler).Initialize(nodeId, null, null, path, FullPath + "/" + path, AccessType);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Path = path,
                Title = LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.NodeAccess),
                Id = nodeId
            };
        }
    }
}
