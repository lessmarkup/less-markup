/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Engine.Cache
{
    class SiteDataCache : IDataCache
    {
        private readonly object _itemsLock = new object();
        private readonly Dictionary<EntityType, List<CacheItem>> _handledEntities = new Dictionary<EntityType, List<CacheItem>>();
        private readonly Dictionary<Tuple<Type, long?>, CacheItem> _items = new Dictionary<Tuple<Type, long?>, CacheItem>();
        private readonly long? _siteId;

        public SiteDataCache(long? siteId)
        {
            _siteId = siteId;
        }

        public DateTime LastAccess { get; set; }

        public void Set<T>(T cachedObject, long? objectId = null, DateTime? expirationTime = null) where T : ICacheHandler
        {
            lock (_itemsLock)
            {
                var key = new Tuple<Type, long?>(typeof(T), objectId);
                var cacheItem = new CacheItem(typeof(T), expirationTime, objectId, cachedObject);

                var exists = _items.ContainsKey(key);

                _items[key] = cacheItem;

                if (exists)
                {
                    return;
                }

                foreach (var entityType in cachedObject.HandledTypes)
                {
                    List<CacheItem> items;
                    if (!_handledEntities.TryGetValue(entityType, out items))
                    {
                        items = new List<CacheItem>();
                        _handledEntities.Add(entityType, items);
                    }
                    items.Add(cacheItem);
                }
            }
        }


        public T Get<T>(long? objectId = null, bool create = true) where T : ICacheHandler
        {
            lock (_itemsLock)
            {
                var key = new Tuple<Type, long?>(typeof(T), objectId);
                CacheItem ret;
                if (_items.TryGetValue(key, out ret))
                {
                    return (T)ret.CachedObject;
                }

                if (!create)
                {
                    return default(T);
                }

                this.LogDebug(string.Format("Cache for site {0}: creating item for type {1}, id {2}", _siteId, typeof(T).Name, objectId ?? (object)"(null)"));
                var newObject = DependencyResolver.Resolve<T>();
                DateTime? expirationTime;
                newObject.Initialize(out expirationTime, objectId);
                Set(newObject, objectId, expirationTime);
                ret = _items[key];
                return (T)ret.CachedObject;
            }
        }

        public T GetGlobal<T>(long? objectId = null, bool create = true) where T : ICacheHandler
        {
            throw new MemberAccessException();
        }

        public void Expired<T>(long? objectId = null) where T : ICacheHandler
        {
            lock (_itemsLock)
            {
                Remove(new Tuple<Type, long?>(typeof(T), objectId));
            }
        }

        public void ExpiredGlobal<T>(long? objectId = null) where T : ICacheHandler
        {
            throw new MemberAccessException();
        }

        public T CreateWithUniqueId<T>() where T : ICacheHandler
        {
            var random = new Random(Environment.TickCount);

            lock (_itemsLock)
            {
                long objectId;

                for (; ; )
                {
                    objectId = random.Next();
                    var key = new Tuple<Type, long?>(typeof(T), objectId);

                    if (!_items.ContainsKey(key))
                    {
                        break;
                    }
                }

                return Get<T>(objectId);
            }
        }

        public T CreateWithUniqueIdGlobal<T>() where T : ICacheHandler
        {
            throw new MemberAccessException();
        }

        public void OnHistoryChanged()
        {
        }

        private void Remove(Tuple<Type, long?> key)
        {
            CacheItem cacheItem;
            if (!_items.TryGetValue(key, out cacheItem))
            {
                return;
            }

            foreach (var entityType in cacheItem.CachedObject.HandledTypes)
            {
                List<CacheItem> items;
                if (_handledEntities.TryGetValue(entityType, out items))
                {
                    items.Remove(cacheItem);
                }
            }

            _items.Remove(key);
        }

        public void UpdateCacheItem(long recordId, long? userId, long entityId, EntityType entityType, EntityChangeType entityChange)
        {
            List<CacheItem> items;

            if (!_handledEntities.TryGetValue(entityType, out items))
            {
                return;
            }

            List<CacheItem> itemsToRemove = null;

            foreach (var item in items.Where(item => item.CachedObject.Expires(entityType, entityId, entityChange)))
            {
                if (itemsToRemove == null)
                {
                    itemsToRemove = new List<CacheItem>();
                }
                itemsToRemove.Add(item);
            }

            if (itemsToRemove == null)
            {
                return;
            }

            foreach (var item in itemsToRemove)
            {
                this.LogDebug(string.Format("Cache for site {0}: removing item of type {1}, id {2}", _siteId, item.Type.Name, item.ObjectId ?? (object)"(null)"));
                Remove(Tuple.Create(item.Type, item.ObjectId));
            }
        }
    }
}
