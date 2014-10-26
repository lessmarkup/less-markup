/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Engine.Cache
{
    class SiteDataCache : IDataCache
    {
        private readonly object _itemsLock = new object();
        private readonly Dictionary<int, List<CacheItem>> _handledCollectionIds = new Dictionary<int, List<CacheItem>>();
        private readonly Dictionary<Tuple<Type, long?>, CacheItem> _items = new Dictionary<Tuple<Type, long?>, CacheItem>();
        private readonly long? _siteId;

        public SiteDataCache(long? siteId)
        {
            _siteId = siteId;
        }

        public DateTime LastAccess { get; set; }

        private void Set<T>(T cachedObject, long? objectId) where T : ICacheHandler
        {
            lock (_itemsLock)
            {
                var key = new Tuple<Type, long?>(typeof(T), objectId);
                var cacheItem = new CacheItem(typeof(T), objectId, cachedObject);

                var exists = _items.ContainsKey(key);

                _items[key] = cacheItem;

                if (exists)
                {
                    return;
                }

                var collectionTypes = cachedObject.HandledCollectionTypes;

                if (collectionTypes != null)
                {
                    foreach (var type in collectionTypes)
                    {
                        var collectionId = AbstractDomainModel.GetCollectionId(type);
                        if (collectionId.HasValue)
                        {
                            List<CacheItem> items;
                            if (!_handledCollectionIds.TryGetValue(collectionId.Value, out items))
                            {
                                items = new List<CacheItem>();
                                _handledCollectionIds.Add(collectionId.Value, items);
                            }
                            items.Add(cacheItem);
                        }
                    }
                }
            }
        }

        public T Get<T>(long? objectId = null, bool create = true) where T : ICacheHandler
        {
            lock (_itemsLock)
            {
                var key = Tuple.Create(typeof(T), objectId);
                CacheItem ret;
                if (_items.TryGetValue(key, out ret))
                {
                    if (ret.CachedObject.Expired)
                    {
                        _items.Remove(key);
                    }
                    else
                    {
                        return (T)ret.CachedObject;
                    }
                }

                if (!create)
                {
                    return default(T);
                }

                this.LogDebug(string.Format("Cache for site {0}: creating item for type {1}, id {2}", _siteId, typeof(T).Name, objectId ?? (object)"(null)"));
                var newObject = DependencyResolver.Resolve<T>();
                newObject.Initialize(_siteId, objectId);
                Set(newObject, objectId);
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
                Remove(Tuple.Create(typeof(T), objectId));
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

        public void Reset()
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

            foreach (var type in cacheItem.CachedObject.HandledCollectionTypes)
            {
                var collectionId = AbstractDomainModel.GetCollectionId(type);
                if (!collectionId.HasValue)
                {
                    continue;
                }
                List<CacheItem> items;
                if (_handledCollectionIds.TryGetValue(collectionId.Value, out items))
                {
                    items.Remove(cacheItem);
                }
            }

            _items.Remove(key);
        }

        public void UpdateCacheItem(long recordId, long? userId, long entityId, int collectionId, EntityChangeType entityChange)
        {
            List<CacheItem> items;

            if (!_handledCollectionIds.TryGetValue(collectionId, out items))
            {
                return;
            }

            List<CacheItem> itemsToRemove = null;

            foreach (var item in items.Where(item => item.CachedObject.Expires(collectionId, entityId, entityChange)))
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
