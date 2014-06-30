/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Framework.Site;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Hubs;

namespace LessMarkup.UserInterface.ChangeTracking
{
    public class RecordChangeTracker
    {
        private readonly object _syncLock = new object();
        private readonly Dictionary<long, TrackerSite> _sites = new Dictionary<long, TrackerSite>();
        private TrackerSite _globalSite;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly ISiteMapper _siteMapper;
        private readonly IChangeTracker _changeTracker;
        private readonly IRequestMapper _requestMapper;

        public RecordChangeTracker(IChangeTracker changeTracker, IDomainModelProvider domainModelProvider, IDataCache dataCache, ISiteMapper siteMapper, IRequestMapper requestMapper)
        {
            RecordListHub.ChangeTracker = this;
            _changeTracker = changeTracker;
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _siteMapper = siteMapper;
            _requestMapper = requestMapper;
        }

        public void Initialize()
        {
            _changeTracker.RecordChanged += OnRecordChanged;
        }

        public bool OnBeginRequest()
        {
            SiteMapperScope.ResetMapping();
            return _requestMapper.MapRequest(_dataCache);
        }

        public void OnEndRequest()
        {
            SiteMapperScope.ResetMapping();
        }

        public TrackerSite Site
        {
            get
            {
                TrackerSite trackerSite;

                var siteId = _siteMapper.SiteId;

                if (!siteId.HasValue)
                {
                    return _globalSite;
                }

                if (!_sites.TryGetValue(siteId.Value, out trackerSite))
                {
                    return null;
                }

                return trackerSite;
            }
        }

        public void RegisterChannel(string channelId, string modelId, string filter)
        {
            TrackerSite trackerSite;

            var siteId = _siteMapper.SiteId;

            lock (_syncLock)
            {
                if (siteId.HasValue)
                {
                    if (!_sites.TryGetValue(siteId.Value, out trackerSite))
                    {
                        trackerSite = new TrackerSite(siteId);
                        _sites[siteId.Value] = trackerSite;
                    }
                }
                else
                {
                    _globalSite = new TrackerSite(siteId);
                    trackerSite = _globalSite;
                }
            }

            trackerSite.RegisterChannel(channelId, modelId, filter, _dataCache);
        }

        public void GetAllIds(string channelId)
        {
            var site = Site;
            if (site != null)
            {
                site.GetAllIds(channelId, _domainModelProvider);
            }
        }

        public void GetRecords(string channelId, List<long> recordIds)
        {
            var site = Site;
            if (site != null)
            {
                site.GetRecords(channelId, recordIds, _domainModelProvider);
            }
        }

        public void DeregisterChannel(string channelId)
        {
            var site = Site;
            if (site != null)
            {
                site.DeregisterChannel(channelId);
            }
        }

        private void OnRecordChanged(long recordId, long? userId, long entityId, EntityType entityType, EntityChangeType entityChange, long? siteId)
        {
            TrackerSite trackerSite;
            if (siteId.HasValue)
            {
                if (!_sites.TryGetValue(siteId.Value, out trackerSite))
                {
                    return;
                }
            }
            else
            {
                trackerSite = _globalSite;
                if (trackerSite == null)
                {
                    return;
                }
            }

            SiteMapperScope.ResetMapping();

            try
            {
                if (!_requestMapper.ForceMapRequest(_dataCache, siteId))
                {
                    return;
                }

                trackerSite.OnRecordChanged(recordId, userId, entityId, entityType, entityChange, _domainModelProvider);
            }
            finally
            {
                SiteMapperScope.ResetMapping();
            }
        }
    }
}
