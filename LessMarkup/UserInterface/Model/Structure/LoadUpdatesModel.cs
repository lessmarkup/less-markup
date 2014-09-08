/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
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
    class LoadUpdatesModel
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public LoadUpdatesModel(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        class NotificationChange
        {
            public long Id { get; set; }
            public int Change { get; set; }
            public int NewValue { get; set; }
        }

        public void Handle(long? versionId, long? newVersionId, string path, Dictionary<string, object> arguments, Dictionary<string, object> returnValues, long? currentNodeId)
        {
            var userCache = _dataCache.Get<IUserCache>(_currentUser.UserId);
            var nodeCache = _dataCache.Get<INodeCache>();

            if ((!newVersionId.HasValue || newVersionId == versionId) && !currentNodeId.HasValue)
            {
                return;
            }

            var handlers = new List<Tuple<long, INotificationProvider>>();

            INotificationProvider currentProvider = null;

            foreach (var node in userCache.Nodes.Where(n => typeof(INotificationProvider).IsAssignableFrom(n.Item1.HandlerType)))
            {
                var handler = (INodeHandler)DependencyResolver.Resolve(node.Item1.HandlerType);
                var notificationProvider = handler as INotificationProvider;
                if (notificationProvider == null)
                {
                    continue;
                }

                if (currentNodeId.HasValue && currentProvider == null && node.Item1.NodeId == currentNodeId)
                {
                    currentProvider = notificationProvider;
                }

                object settings = null;
                if (!string.IsNullOrEmpty(node.Item1.Settings))
                {
                    settings = JsonConvert.DeserializeObject(node.Item1.Settings);
                }

                handler.Initialize(node.Item1.NodeId, settings, null, node.Item1.Path, node.Item1.FullPath, node.Item2);
                handlers.Add(Tuple.Create(node.Item1.NodeId, notificationProvider));
            }

            if ((!newVersionId.HasValue || newVersionId == versionId) && currentProvider == null)
            {
                return;
            }

            var notificationChanges = new List<NotificationChange>();

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var handler in handlers)
                {
                    if (handler.Item2 == null)
                    {
                        continue;
                    }

                    if (handler.Item2 == currentProvider && currentProvider != null)
                    {
                        var change = handler.Item2.GetValueChange(null, newVersionId, domainModel);
                        notificationChanges.Add(new NotificationChange { Id = handler.Item1, NewValue = change });
                    }
                    else
                    {
                        var change = handler.Item2.GetValueChange(versionId, newVersionId, domainModel);

                        if (change > 0)
                        {
                            notificationChanges.Add(new NotificationChange { Id = handler.Item1, Change = change });
                        }
                    }
                }

                var currentHandler = nodeCache.GetNodeHandler(path);

                if (currentHandler != null && newVersionId.HasValue)
                {
                    var updates = new Dictionary<string, object>();
                    currentHandler.ProcessUpdates(versionId, newVersionId.Value, updates, domainModel, arguments);
                    if (updates.Count > 0)
                    {
                        returnValues["updates"] = updates;
                    }
                }
            }

            if (notificationChanges.Count > 0)
            {
                returnValues["notificationChanges"] = notificationChanges;
            }
        }
    }
}
