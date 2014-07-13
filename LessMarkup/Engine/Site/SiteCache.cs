/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.Site
{
    public class SiteCache : ICacheHandler
    {
        private readonly EntityType[] _handledTypes = {EntityType.Site};
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly List<SiteCacheItem> _sites = new List<SiteCacheItem>();
        private readonly Dictionary<string, SiteCacheItem> _sitesByHost = new Dictionary<string, SiteCacheItem>();
        private readonly Dictionary<long, SiteCacheItem> _sitesById = new Dictionary<long, SiteCacheItem>();
        private readonly IModuleProvider _moduleProvider;

        public SiteCache(IDomainModelProvider domainModelProvider, IModuleProvider moduleProvider)
        {
            _domainModelProvider = domainModelProvider;
            _moduleProvider = moduleProvider;
        }

        private List<string> GetSystemModuleTypes()
        {
            return _moduleProvider.Modules.Where(m => m.System).Select(m => m.ModuleType).ToList();
        }


        public void Initialize(long? siteId, out DateTime? expirationTime, long? objectId = null)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            expirationTime = null;
            var systemModuleTypes = GetSystemModuleTypes();

            using (var domainModel = _domainModelProvider.Create(null))
            {
                foreach (var site in domainModel.GetCollection<Interfaces.Data.Site>().Select(s => new
                {
                    s.SiteId,
                    s.Host,
                    s.Title,
                    s.Enabled,
                    ModuleTypes = s.Modules.Select(m => m.Module.ModuleType),
                    Properties = s.Properties.Select(p => new {p.Name, p.Value})
                }))
                {
                    var cacheItem = new SiteCacheItem
                    {
                        Enabled = site.Enabled,
                        Hosts = new HashSet<string>((site.Host ?? "").ToLower().Split(new []{' '})),
                        SiteId = site.SiteId,
                        Title = site.Title,
                        Properties = site.Properties.ToDictionary(p => p.Name, p => p.Value),
                        ModuleTypes = new HashSet<string>(site.ModuleTypes)
                    };

                    // System modules shoule be always enabled

                    foreach (var moduleType in systemModuleTypes)
                    {
                        if (!cacheItem.ModuleTypes.Contains(moduleType))
                        {
                            cacheItem.ModuleTypes.Add(moduleType);
                        }
                    }

                    if (cacheItem.Hosts.Count == 0)
                    {
                        continue;
                    }

                    _sites.Add(cacheItem);

                    foreach (var host in cacheItem.Hosts)
                    {
                        _sitesByHost[host] = cacheItem;
                    }

                    _sitesById[site.SiteId] = cacheItem;
                }
            }
        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.Site;
        }

        public EntityType[] HandledTypes { get { return _handledTypes; } }

        public SiteCacheItem GetByHostName(string hostName)
        {
            SiteCacheItem item;
            if (!_sitesByHost.TryGetValue(hostName.ToLower(), out item))
            {
                return null;
            }
            return item;
        }

        public bool HasAnySite
        {
            get { return _sites.Any(); }
        }

        public SiteCacheItem GetBySiteId(long siteId)
        {
            SiteCacheItem item;
            if (!_sitesById.TryGetValue(siteId, out item))
            {
                return null;
            }
            return item;
        }
    }
}
