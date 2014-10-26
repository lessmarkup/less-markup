/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Engine.Logging;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Cache
{
    class DataCache : IDataCache
    {
        #region Private Fields

        private readonly object _siteCachesLock = new object();
        private readonly Dictionary<long, SiteDataCache> _siteCaches = new Dictionary<long, SiteDataCache>();
        private SiteDataCache _nullCache;
        private SiteDataCache _globalCache;
        private readonly ISiteMapper _siteMapper;
        private readonly IChangeTracker _changeTracker;
        private bool _subscribedToChanges;

        #endregion

        public DataCache(ISiteMapper siteMapper, IChangeTracker changeTracker)
        {
            _siteMapper = siteMapper;
            _changeTracker = changeTracker;
        }

        private void CheckSubscribeToChanges()
        {
            if (_subscribedToChanges)
            {
                return;
            }

            lock (_siteCachesLock)
            {
                if (_subscribedToChanges)
                {
                    return;
                }
                _subscribedToChanges = true;
                _changeTracker.RecordChanged += UpdateCacheItem;
            }
        }

        private SiteDataCache SiteCache
        {
            get
            {
                CheckSubscribeToChanges();

                var siteId = _siteMapper.SiteId;

                lock (_siteCachesLock)
                {
                    SiteDataCache ret;
                    if (siteId.HasValue)
                    {
                        if (!_siteCaches.TryGetValue(siteId.Value, out ret))
                        {
                            ret = new SiteDataCache(siteId.Value);
                            _siteCaches[siteId.Value] = ret;
                        }
                    }
                    else
                    {
                        if (_nullCache == null)
                        {
                            _nullCache = new SiteDataCache(null);
                        }
                        ret = _nullCache;
                    }
                    ret.LastAccess = DateTime.UtcNow;
                    return ret;
                }
            }
        }

        private SiteDataCache GlobalCache
        {
            get
            {
                CheckSubscribeToChanges();

                if (_globalCache == null)
                {
                    _globalCache = new SiteDataCache(null);
                }
                _globalCache.LastAccess = DateTime.UtcNow;
                return _globalCache;
            }            
        }

        public void Expired<T>(long? objectId = null) where T : ICacheHandler
        {
            SiteCache.Expired<T>(objectId);
        }

        public void ExpiredGlobal<T>(long? objectId = null) where T : ICacheHandler
        {
            GlobalCache.Expired<T>(objectId);
        }

        public T CreateWithUniqueId<T>() where T : ICacheHandler
        {
            return SiteCache.CreateWithUniqueId<T>();
        }

        public T CreateWithUniqueIdGlobal<T>() where T : ICacheHandler
        {
            return GlobalCache.CreateWithUniqueId<T>();
        }

        public void Reset()
        {
            lock (_siteCachesLock)
            {
                _globalCache = null;
                _nullCache = null;
                _siteCaches.Clear();
            }
        }

        public T Get<T>(long? objectId = null, bool create = true) where T : ICacheHandler
        {
            return SiteCache.Get<T>(objectId, create);
        }

        public T GetGlobal<T>(long? objectId = null, bool create = true) where T : ICacheHandler
        {
            return GlobalCache.Get<T>(objectId, create);
        }

        private void UpdateCacheItem(long recordId, long? userId, long entityId, int collectionId, EntityChangeType entityChange, long? siteId)
        {
            if (_globalCache != null)
            {
                this.LogDebug(string.Format("Handling data change item for global site, id={0}", recordId));
                _globalCache.UpdateCacheItem(recordId, userId, entityId, collectionId, entityChange);
            }

            if (!siteId.HasValue)
            {
                this.LogDebug(string.Format("Handling data change item for null site, id={0}", recordId));

                if (_nullCache != null)
                {
                    _nullCache.UpdateCacheItem(recordId, userId, entityId, collectionId, entityChange);
                }

                return;
            }

            if (collectionId == AbstractDomainModel.GetCollectionId<Interfaces.Data.Site>())
            {
                this.LogDebug(string.Format("Detected site change, removing complete site configuration for site {0}", entityId));
                lock (_siteCachesLock)
                {
                    _siteCaches.Remove(entityId);
                }
                return;
            }

            this.LogDebug(string.Format("Handled data change item for site id={0}, siteid={1}", recordId, siteId.Value));
            SiteDataCache siteDataCache;
            if (_siteCaches.TryGetValue(siteId.Value, out siteDataCache))
            {
                siteDataCache.UpdateCacheItem(recordId, userId, entityId, collectionId, entityChange);
            }
        }
    }
}
