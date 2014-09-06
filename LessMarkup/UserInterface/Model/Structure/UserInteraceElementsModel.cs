using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class UserInteraceElementsModel
    {
        class NotificationInfo
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public string Tooltip { get; set; }
            public string Icon { get; set; }
            public string Path { get; set; }
            public int Count { get; set; }
        }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public UserInteraceElementsModel(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        private void FillNavigationBarItems(IEnumerable<ICachedNodeInformation> nodes, int level, List<MenuItemModel> menuItems)
        {
            foreach (var node in nodes)
            {
                if (!node.Visible)
                {
                    continue;
                }

                var accessType = node.CheckRights(_currentUser);

                if (accessType == NodeAccessType.NoAccess)
                {
                    continue;
                }

                var model = new MenuItemModel
                {
                    Title = node.Title,
                    Url = node.FullPath,
                    Level = level,
                };

                menuItems.Add(model);

                FillNavigationBarItems(node.Children, level + 1, menuItems);
            }
        }

        public void Handle(Dictionary<string, object> returnValues, object controller, long? lastChangeId)
        {
            var notifications = new List<NotificationInfo>();
            var nodeCache = _dataCache.Get<INodeCache>();
            var siteConfiguration = _dataCache.Get<ISiteConfiguration>();

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var nodeInfo in nodeCache.Nodes.Where(n => typeof(INotificationProvider).IsAssignableFrom(n.HandlerType)))
                {
                    var accessType = nodeInfo.CheckRights(_currentUser);

                    if (accessType == NodeAccessType.NoAccess)
                    {
                        continue;
                    }

                    var node = (INodeHandler)DependencyResolver.Resolve(nodeInfo.HandlerType);
                    object settings = null;
                    if (!string.IsNullOrEmpty(nodeInfo.Settings))
                    {
                        settings = JsonConvert.DeserializeObject(nodeInfo.Settings);
                    }

                    node.Initialize(nodeInfo.NodeId, settings, controller, nodeInfo.Path, nodeInfo.FullPath, accessType);

                    var notificationProvider = node as INotificationProvider;

                    if (notificationProvider != null)
                    {
                        var notificationInfo = new NotificationInfo
                        {
                            Id = nodeInfo.NodeId,
                            Title = notificationProvider.Title,
                            Tooltip = notificationProvider.Tooltip,
                            Icon = notificationProvider.Icon,
                            Path = nodeInfo.FullPath,
                            Count = notificationProvider.GetValueChange(null, lastChangeId, domainModel)
                        };

                        notifications.Add(notificationInfo);
                    }
                }
            }

            var menuNodes = nodeCache.Nodes.Where(n => n.AddToMenu && n.Visible).Select(n => new MenuItemModel
            {
                Title = n.Title,
                Url = n.FullPath
            }).ToList();

            returnValues["topMenu"] = menuNodes;
            returnValues["notifications"] = notifications;

            if (siteConfiguration.HasNavigationBar)
            {
                var navigationTree = new List<MenuItemModel>();
                FillNavigationBarItems(nodeCache.RootNode.Children, 0, navigationTree);
                returnValues["navigationTree"] = navigationTree;
            }
        }
    }
}
