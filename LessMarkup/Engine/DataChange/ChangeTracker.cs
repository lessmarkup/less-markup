/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.DataChange
{
    class ChangeTracker : IChangeTracker
    {
        private volatile bool _changeTrackingInitialized;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly object _syncLock = new object();
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly ICurrentUser _currentUser;
        private readonly ISiteMapper _siteMapper;
        private long _lastUpdateId;
        private readonly Timer _triggerTimer;
        private readonly Timer _queueTimer;
        private Timer _periodTimer;
        private bool _timerStarted;

        private readonly Queue<EntityChangeHistory> _changeQueue = new Queue<EntityChangeHistory>();
        private readonly object _syncChangeQueue = new object();

        public ChangeTracker(IDomainModelProvider domainModelProvider, IEngineConfiguration engineConfiguration, ISiteMapper siteMapper, ICurrentUser currentUser)
        {
            _currentUser = currentUser;
            _domainModelProvider = domainModelProvider;
            _engineConfiguration = engineConfiguration;
            _siteMapper = siteMapper;
            _triggerTimer = new Timer(SendUpdates);
            _queueTimer = new Timer(HandleQueue);

            _queueTimer.Change(200, Timeout.Infinite);
        }

        public void Stop()
        {
            _triggerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (_periodTimer != null)
            {
                _periodTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void InitializeChangeTracking()
        {
            if (_changeTrackingInitialized)
            {
                return;
            }

            lock (_syncLock)
            {
                if (_changeTrackingInitialized)
                {
                    return;
                }

                _changeTrackingInitialized = true;

                if (string.IsNullOrWhiteSpace(_engineConfiguration.Database))
                {
                    this.LogDebug("Cannot initialize change tracking due to empty database configuration");
                    return;
                }

                bool initialized = false;

                if (_engineConfiguration.UseChangeTracking)
                {
                    try
                    {
                        using (var model = _domainModelProvider.Create(null))
                        {
                            var sql = model.GetCollection<EntityChangeHistory>().OrderByDescending(c => c.Created).Select(c => new {c.Id}).ToString();

                            var connection = new SqlConnection(_engineConfiguration.Database);
                            connection.Open();
                            var command = connection.CreateCommand();
                            command.CommandText = sql;

                            SqlDependency.Start(_engineConfiguration.Database);

                            var dependency = new SqlDependency(command);
                            dependency.OnChange += OnDataChanged;

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    _lastUpdateId = reader.GetInt64(0);
                                }
                            }
                        }

                        initialized = true;
                    }
                    catch (Exception e)
                    {
                        this.LogDebug("Cannot initialize change tracking: " + e.Message);
                    }
                }

                if (!initialized)
                {
                    using (var model = _domainModelProvider.Create(null))
                    {
                        var lastChange = model.GetCollection<EntityChangeHistory>().OrderByDescending(c => c.Created).Select(c => new {c.Id}).FirstOrDefault();
                        if (lastChange != null)
                        {
                            _lastUpdateId = lastChange.Id;
                        }
                    }

                    _periodTimer = new Timer(SendUpdates, null, 400, 400);
                }
            }
        }

        private void OnDataChanged(object sender, SqlNotificationEventArgs eventArgs)
        {
            if (eventArgs.Type != SqlNotificationType.Change)
            {
                return;
            }

            if (RecordChangedInternal == null)
            {
                return;
            }

            if (_timerStarted)
            {
                return;
            }

            lock (_syncLock)
            {
                if (_timerStarted)
                {
                    return;
                }

                _timerStarted = true;
                _triggerTimer.Change(400, Timeout.Infinite);
            }
        }

        private void SendUpdates(object sender)
        {
            _timerStarted = false;

            using (var model = _domainModelProvider.Create(null))
            {
                foreach (var change in model.GetCollection<EntityChangeHistory>().Where(c => c.Id > _lastUpdateId).OrderBy(c => c.Id))
                {
                    _lastUpdateId = change.Id;
                    lock (_syncChangeQueue)
                    {
                        _changeQueue.Enqueue(change);
                    }
                }
            }
        }

        public void Invalidate()
        {
            SendUpdates(null);
            HandleQueue(null);
        }

        private void HandleQueue(object sender)
        {
            try
            {
                var recordChanged = RecordChangedInternal;

                if (recordChanged == null)
                {
                    _queueTimer.Change(200, Timeout.Infinite);
                    return;
                }

                for (; ; )
                {
                    EntityChangeHistory change;

                    lock (_syncChangeQueue)
                    {
                        if (_changeQueue.Count == 0)
                        {
                            break;
                        }

                        change = _changeQueue.Dequeue();
                    }

                    this.LogDebug(string.Format("ChangeTracker: detected {0}/{1}, change {2}", change.CollectionId, change.EntityId, (EntityChangeType)change.ChangeType));
                    recordChanged(change.Id, change.UserId, change.EntityId, change.CollectionId, (EntityChangeType)change.ChangeType, change.SiteId);
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
            }

            _queueTimer.Change(200, Timeout.Infinite);
        }

        public void AddChange<T>(long objectId, EntityChangeType changeType, IDomainModel domainModel) where T : IDataObject
        {
            var collectionId = AbstractDomainModel.GetCollectionId<T>();

            this.LogDebug(string.Format("ChangeTracker: recorded {0}/{1}, change {2}", collectionId, objectId, changeType));

            var userId = _currentUser.UserId;

            if (_currentUser.IsFakeUser)
            {
                userId = null;
            }

            var history = new EntityChangeHistory
            {
                ChangeType = (int)changeType,
                Created = DateTime.UtcNow,
                EntityId = objectId,
                CollectionId = collectionId,
                SiteId = _siteMapper.SiteId,
                UserId = userId
            };

            domainModel.GetCollection<EntityChangeHistory>().Add(history);
        }

        public void AddChange<T>(T dataObject, EntityChangeType changeType, IDomainModel domainModel) where T : IDataObject
        {
            AddChange<T>(dataObject.Id, changeType, domainModel);
        }

        private event RecordChangeHandler RecordChangedInternal;

        public event RecordChangeHandler RecordChanged
        {
            add
            {
                InitializeChangeTracking();
                RecordChangedInternal += value;
            }
            remove
            {
                RecordChangedInternal -= value;
            }
        }
    }
}
