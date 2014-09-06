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
        }

        public void Handle(long? versionId, long? newVersionId, string path, Dictionary<string, object> arguments, Dictionary<string, object> returnValues)
        {
            var userCache = _dataCache.Get<IUserCache>(_currentUser.UserId);
            var nodeCache = _dataCache.Get<INodeCache>();

            if (!newVersionId.HasValue || newVersionId == versionId)
            {
                return;
            }

            var handlers = new List<Tuple<long, INotificationProvider>>();

            foreach (var node in userCache.Nodes.Where(n => typeof(INotificationProvider).IsAssignableFrom(n.Item1.HandlerType)))
            {
                var handler = (INodeHandler)DependencyResolver.Resolve(node.Item1.HandlerType);
                var notificationProvider = handler as INotificationProvider;
                if (notificationProvider == null)
                {
                    continue;
                }
                object settings = null;
                if (!string.IsNullOrEmpty(node.Item1.Settings))
                {
                    settings = JsonConvert.DeserializeObject(node.Item1.Settings);
                }

                handler.Initialize(node.Item1.NodeId, settings, null, node.Item1.Path, node.Item1.FullPath, node.Item2);
                handlers.Add(Tuple.Create(node.Item1.NodeId, notificationProvider));
            }

            var notificationChanges = new List<NotificationChange>();

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var handler in handlers)
                {
                    var change = handler.Item2.GetValueChange(versionId, newVersionId, domainModel);

                    if (change > 0)
                    {
                        notificationChanges.Add(new NotificationChange { Id = handler.Item1, Change = change });
                    }
                }

                var currentHandler = nodeCache.GetNodeHandler(path);

                if (currentHandler != null)
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
