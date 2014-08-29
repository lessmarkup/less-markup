/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NotificationsModel
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public class NotificationRequest
        {
            public long Id { get; set; }
            public long? Version { get; set; }
        }

        public class NotificationResponse
        {
            public long Id { get; set; }
            public int Count { get; set; }
            public long? Version { get; set; }
        }

        public NotificationsModel(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public object Handle(string data, object controller)
        {
            var requests = JsonConvert.DeserializeObject<List<NotificationRequest>>(data);

            var responses = new List<NotificationResponse>();

            var nodeCache = _dataCache.Get<INodeCache>();

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var request in requests)
                {
                    var nodeInfo = nodeCache.GetNode(request.Id);

                    if (!typeof (INotificationProvider).IsAssignableFrom(nodeInfo.HandlerType))
                    {
                        continue;
                    }

                    var accessType = nodeInfo.CheckRights(_currentUser);

                    if (accessType == NodeAccessType.NoAccess)
                    {
                        continue;
                    }

                    var handler = (INodeHandler) DependencyResolver.Resolve(nodeInfo.HandlerType);

                    object settings = null;
                    if (!string.IsNullOrEmpty(nodeInfo.Settings))
                    {
                        settings = JsonConvert.DeserializeObject(nodeInfo.Settings);
                    }

                    handler.Initialize(nodeInfo.NodeId, settings, controller, nodeInfo.Path, nodeInfo.FullPath, accessType);

                    var countAndVersion = ((INotificationProvider) handler).GetCountAndVersion(request.Version, domainModel);

                    responses.Add(new NotificationResponse
                    {
                        Count = countAndVersion.Item1,
                        Id = request.Id,
                        Version = countAndVersion.Item2
                    });
                }
            }

            return new
            {
                notifications = responses
            };
        }
    }
}
