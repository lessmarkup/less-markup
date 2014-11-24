/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.Site
{
    public class SiteCache : AbstractCacheHandler
    {
        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IModuleProvider _moduleProvider;

        public string Title { get; private set; }
        public bool Enabled { get; private set; }
        public string Properties { get; private set; }
        public HashSet<string> ModuleTypes { get; private set; }

        public SiteCache(ILightDomainModelProvider domainModelProvider, IModuleProvider moduleProvider)
            : base(new[] { typeof(SiteProperties)})
        {
            _domainModelProvider = domainModelProvider;
            _moduleProvider = moduleProvider;
        }

        private IEnumerable<string> GetSystemModuleTypes()
        {
            return _moduleProvider.Modules.Where(m => m.System).Select(m => m.ModuleType).ToList();
        }

        protected override void Initialize(long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            var systemModuleTypes = GetSystemModuleTypes();

            using (var domainModel = _domainModelProvider.Create())
            {
                var siteProperties = domainModel.Query().From<SiteProperties>().FirstOrDefault<SiteProperties>();

                if (siteProperties != null)
                {
                    Title = siteProperties.Title;
                    Enabled = siteProperties.Enabled;
                    Properties = siteProperties.Properties;
                }

                ModuleTypes = new HashSet<string>(domainModel.Query().From<Interfaces.Data.Module>().Where("Enabled = $", true).ToList<Interfaces.Data.Module>().Select(m => m.ModuleType));

                foreach (var moduleType in systemModuleTypes)
                {
                    if (!ModuleTypes.Contains(moduleType))
                    {
                        ModuleTypes.Add(moduleType);
                    }
                }
            }
        }
    }
}
