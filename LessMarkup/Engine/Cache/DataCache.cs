/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Engine.Cache
{
    class DataCache : IDataCache
    {
        #region Private Fields

        private readonly object _siteCachesLock = new object();
        private readonly IChangeTracker _changeTracker;
        private bool _subscribedToChanges;
        private SiteDataCache _siteDataCache;

        #endregion

        public DataCache(IChangeTracker changeTracker)
        {
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

                if (_siteDataCache == null)
                {
                    lock (_siteCachesLock)
                    {
                        if (_siteDataCache == null)
                        {
                            _siteDataCache = new SiteDataCache();
                        }

                    }
                }

                _siteDataCache.LastAccess = DateTime.UtcNow;
                return _siteDataCache;
            }
        }

        public void Expired<T>(long? objectId = null) where T : ICacheHandler
        {
            SiteCache.Expired<T>(objectId);
        }

        public T CreateWithUniqueId<T>() where T : ICacheHandler
        {
            return SiteCache.CreateWithUniqueId<T>();
        }

        public void Reset()
        {
            lock (_siteCachesLock)
            {
                _siteDataCache = null;
            }
        }

        public T Get<T>(long? objectId = null, bool create = true) where T : ICacheHandler
        {
            return SiteCache.Get<T>(objectId, create);
        }

        private void UpdateCacheItem(long recordId, long? userId, long entityId, int collectionId, EntityChangeType entityChange)
        {
            this.LogDebug(string.Format("Handled data change item for site id={0}", recordId));

            var siteDataCache = SiteCache;

            if (siteDataCache != null)
            {
                siteDataCache.UpdateCacheItem(recordId, userId, entityId, collectionId, entityChange);
            }
        }
    }
}
