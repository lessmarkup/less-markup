/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Engine.DataChange
{
    class ChangesCache : AbstractCacheHandler, IChangesCache
    {
        class Change : IDataChange
        {
            public long Id { get; set; }
            public long EntityId { get; set; }
            public DateTime Created { get; set; }
            public EntityChangeType Type { get; set; }
            public long? UserId { get; set; }
            public long Parameter1 { get; set; }
            public long Parameter2 { get; set; }
            public long Parameter3 { get; set; }
        }

        private long _siteId;
        private long? _lastUpdateId;
        private long _lastUpdateTime;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly IDomainModelProvider _domainModelProvider;
        private const int UpdateInterval = 500;
        private readonly Dictionary<int, List<Change>> _changes = new Dictionary<int, List<Change>>();

        public ChangesCache(IDomainModelProvider domainModelProvider) : base(null)
        {
            _domainModelProvider = domainModelProvider;
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (!siteId.HasValue)
            {
                throw new ArgumentNullException("siteId");
            }

            if (objectId.HasValue)
            {
                throw new ArgumentException("objectId");
            }

            _siteId = siteId.Value;
        }

        private void UpdateIfRequired()
        {
            if (Environment.TickCount - _lastUpdateTime <= UpdateInterval)
            {
                return;
            }

            _lock.EnterWriteLock();

            try
            {
                if (Environment.TickCount - _lastUpdateTime <= UpdateInterval)
                {
                    return;
                }

                _lastUpdateTime = Environment.TickCount;

                using (var domainModel = _domainModelProvider.Create())
                {
                    var dateFrame = DateTime.UtcNow.AddHours(-24);
                    var query = domainModel.GetCollection<EntityChangeHistory>().Where(c => c.SiteId.HasValue && c.SiteId.Value == _siteId && c.Created >= dateFrame);
                    if (_lastUpdateId.HasValue)
                    {
                        query = query.Where(c => c.Id > _lastUpdateId.Value);
                    }
                    foreach (var item in query)
                    {
                        _lastUpdateId = item.Id;
                        List<Change> collection;
                        if (!_changes.TryGetValue(item.CollectionId, out collection))
                        {
                            collection = new List<Change>();
                            _changes[item.CollectionId] = collection;
                        }
                        collection.Add(new Change
                        {
                            Id = item.Id,
                            EntityId = item.EntityId,
                            Created = item.Created,
                            Type = (EntityChangeType) item.ChangeType,
                            UserId = item.UserId,
                            Parameter1 = item.Parameter1,
                            Parameter2 = item.Parameter2,
                            Parameter3 = item.Parameter3
                        });
                        if (collection[0].Created < dateFrame)
                        {
                            collection.RemoveAll(c => c.Created < dateFrame);
                        }
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerable<IDataChange> GetCollectionChanges(int collectionId, long? fromId, long? toId, Func<IDataChange, bool> filterFunc = null)
        {
            UpdateIfRequired();

            _lock.EnterReadLock();
            try
            {
                List<Change> collection;
                if (!_changes.TryGetValue(collectionId, out collection))
                {
                    return null;
                }

                var query = collection.AsQueryable();

                if (fromId.HasValue)
                {
                    query = query.Where(c => c.Id > fromId.Value);
                }

                if (toId.HasValue)
                {
                    query = query.Where(c => c.Id <= toId.Value);
                }

                if (filterFunc != null)
                {
                    query = query.Where(c => filterFunc(c));
                }

                return query.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public long? LastChangeId
        {
            get
            {
                UpdateIfRequired();
                return _lastUpdateId;
            }
        }
    }
}
