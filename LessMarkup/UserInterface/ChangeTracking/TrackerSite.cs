/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.UserInterface.ChangeTracking
{
    public class TrackerSite
    {
        private readonly long? _siteId;
        private readonly object _syncLock = new object();
        private readonly Dictionary<string, TrackerRecordType> _recordChannels = new Dictionary<string, TrackerRecordType>();
        private readonly Dictionary<EntityType, List<TrackerRecordType>> _entityToChannel = new Dictionary<EntityType, List<TrackerRecordType>>();

        public TrackerSite(long? siteId)
        {
            _siteId = siteId;
        }

        public void RegisterChannel(string channelId, string modelId, string filter, IDataCache dataCache)
        {
            lock (_syncLock)
            {
                var recordChannel = TrackerRecordType.Create(channelId, modelId, filter, dataCache);
                _recordChannels[channelId] = recordChannel;
                if (recordChannel.EntityType != EntityType.None)
                {
                    List<TrackerRecordType> entityChannels;
                    if (!_entityToChannel.TryGetValue(recordChannel.EntityType, out entityChannels))
                    {
                        entityChannels = new List<TrackerRecordType>();
                        _entityToChannel[recordChannel.EntityType] = entityChannels;
                    }
                    entityChannels.Add(recordChannel);
                }
            }
        }

        public void DeregisterChannel(string channelId)
        {
            lock (_syncLock)
            {
                var channel = _recordChannels[channelId];
                _recordChannels.Remove(channelId);
                List<TrackerRecordType> entityChannels;
                if (_entityToChannel.TryGetValue(channel.EntityType, out entityChannels))
                {
                    entityChannels.Remove(channel);
                }
            }
        }

        public void GetAllIds(string channelId, IDomainModelProvider domainModelProvider)
        {
            TrackerRecordType channel;

            lock (_syncLock)
            {
                if (!_recordChannels.TryGetValue(channelId, out channel))
                {
                    return;
                }
            }

            channel.GetAllIds(domainModelProvider);
        }

        public void GetRecords(string channelId, List<long> recordIds, IDomainModelProvider domainModelProvider)
        {
            TrackerRecordType channel;

            lock (_syncLock)
            {
                if (!_recordChannels.TryGetValue(channelId, out channel))
                {
                    return;
                }
            }

            channel.GetRecords(recordIds, domainModelProvider);
        }

        public void OnRecordChanged(long recordId, long? userId, long entityId, EntityType entityType, EntityChangeType entityChange, IDomainModelProvider domainModelProvider)
        {
            List<TrackerRecordType> entityChannels;
            if (_entityToChannel.TryGetValue(entityType, out entityChannels) && entityChannels.Count > 0)
            {
                using (var domainModel = domainModelProvider.Create())
                {
                    entityChannels = entityChannels.ToList();
                    foreach (var channel in entityChannels)
                    {
                        channel.OnRecordChanged(recordId, entityId, entityChange, domainModel);
                    }
                }
            }
        }

        public long? SiteId { get { return _siteId; } }
    }
}
