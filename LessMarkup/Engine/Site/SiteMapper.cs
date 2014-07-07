/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Site
{
    class SiteMapper : ISiteMapper, IRequestMapper, IInitialize
    {
        private bool _initialized;

        private readonly IModuleProvider _moduleProvider;
        private readonly IEngineConfiguration _engineConfiguration;

        private readonly static ThreadLocal<SiteCacheItem> _threadSiteId = new ThreadLocal<SiteCacheItem>();

        public SiteMapper(IModuleProvider moduleProvider, IEngineConfiguration engineConfiguration)
        {
            _moduleProvider = moduleProvider;
            _engineConfiguration = engineConfiguration;
        }

        private HashSet<string> GetSystemModuleTypes()
        {
            return new HashSet<string>(_moduleProvider.Modules.Where(m => m.System).Select(m => m.ModuleType));
        }

        private void OnSiteMapped(SiteCacheItem site)
        {
            if (site != null)
            {
                this.LogDebug("Mapped to site '" + site.Title + "', id " + site.SiteId);
            }
        }

        public static bool IsMappingSet()
        {
            return _threadSiteId.Value != null;
        }

        public static void ResetMapping()
        {
            _threadSiteId.Value = null;
        }

        public bool MapRequest(IDataCache dataCache)
        {
            var site = _threadSiteId.Value;

            if (site != null)
            {
                return site.SiteId.HasValue;
            }

            string hostName;

            if (!ParseHostName(out hostName) || hostName == null)
            {
                this.LogDebug("Cannot parse host name, ignoring site mapping");
                _threadSiteId.Value = new SiteCacheItem();
                return false;
            }

            this.LogDebug("Hostname recognized as '" + hostName + "'");

            var siteCache = dataCache.GetGlobal<SiteCache>();
            site = siteCache.GetByHostName(hostName);

            if (site != null && !site.Enabled)
            {
                this.LogDebug("Site is disabled, ignoring request");
                _threadSiteId.Value = new SiteCacheItem();
                return false;
            }

            if (site != null)
            {
                _threadSiteId.Value = site;
                OnSiteMapped(site);
                return true;
            }

            site = InitializeSite(hostName, siteCache);

            if (site == null || !site.Enabled)
            {
                this.LogDebug("Cannot find site mapping, ignoring request");
                _threadSiteId.Value = new SiteCacheItem();
                return false;
            }

            _threadSiteId.Value = site;

            OnSiteMapped(site);

            return site.Enabled;
        }

        public bool ForceMapRequest(IDataCache dataCache, long? siteId)
        {
            var site = _threadSiteId.Value;

            if (site != null)
            {
                return site.Enabled;
            }

            if (!siteId.HasValue)
            {
                site = new SiteCacheItem
                {
                    Enabled = true,
                    ModuleTypes = GetSystemModuleTypes(),
                    Hosts = new HashSet<string>(new[] {_engineConfiguration.DefaultHostName}),
                    Title = _engineConfiguration.DefaultHostName
                };
            }
            else
            {
                site = dataCache.GetGlobal<SiteCache>().GetBySiteId(siteId.Value);
            }

            if (site == null)
            {
                this.LogDebug("Cannot find site mapping, ignoring request");
                _threadSiteId.Value = new SiteCacheItem();
                return false;
            }

            if (!site.Enabled)
            {
                this.LogDebug("Site is disabled, ignoring request");
                _threadSiteId.Value = new SiteCacheItem();
                return false;
            }

            _threadSiteId.Value = site;
            return true;
        }

        private SiteCacheItem InitializeSite(string hostName, SiteCache siteCache)
        {
            if (!string.IsNullOrEmpty(hostName) && siteCache.HasAnySite)
            {
                var defaultHostName = _engineConfiguration.DefaultHostName;

                if (!string.IsNullOrWhiteSpace(defaultHostName) && hostName != defaultHostName)
                {
                    this.LogDebug("Mapping for site '" + hostName + "' not found, ignoring request");
                    return null;
                }
            }

            this.LogDebug("Mapping not found, creating default mapping with only system modules enabled");

            return new SiteCacheItem
            {
                Enabled = true,
                ModuleTypes = GetSystemModuleTypes(),
                Hosts = new HashSet<string>(new[]{hostName}),
                Title = hostName
            };
        }

        private bool ParseHostName(out string hostName)
        {
            hostName = HttpContext.Current.Request.Headers["host"] ?? "";

            if (hostName.Length > 255)
            {
                this.LogDebug("Host name is too long");
                return false;
            }

            string hostNameNoPort = null;

            for (var i = 0; i < hostName.Length; i++)
            {
                var c = hostName[i];

                if (hostNameNoPort != null)
                {
                    if (c >= '0' && c <= '9')
                    {
                        continue;
                    }

                this.LogDebug("Unknown char '" + c + "in the port number");
                    return false;
                }

                if (c == ':')
                {
                    hostNameNoPort = hostName.Substring(0, i);
                    continue;
                }

                if (c == '-' || c == '.' || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    continue;
                }

                this.LogDebug("Unknown char '" + c + "in the hostname");

                return false;
            }

            return true;
        }

        public void Reset()
        {
        }

        private SiteCacheItem GetCurrentSite()
        {
            if (!_initialized)
            {
                return null;
            }

            var site = _threadSiteId.Value;
            if (site == null)
            {
                return null;
            }

            return site;
        }

        public long? SiteId
        {
            get
            {
                var currentSite = GetCurrentSite();
                return currentSite != null ? currentSite.SiteId : null;
            }
        }

        public string Title
        {
            get
            {
                var currentSite = GetCurrentSite();
                return currentSite != null ? currentSite.Title : "";
            }
        }

        public IEnumerable<string> ModuleTypes
        {
            get
            {
                var currentSite = GetCurrentSite();
                if (currentSite == null || !currentSite.Enabled)
                {
                    return null;
                }
                return currentSite.ModuleTypes;
            }
        }

        public bool ModuleEnabled(string moduleType)
        {
            var currentSite = GetCurrentSite();
            return currentSite != null && currentSite.Enabled && currentSite.ModuleTypes.Contains(moduleType);
        }

        public void Initialize(params object[] arguments)
        {
            _initialized = true;
        }
    }
}
