/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

namespace LessMarkup.Engine.Configuration
{
    class SiteConfigurationCache : AbstractCacheHandler, ISiteConfiguration
    {
        #region Private Fields

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISiteMapper _siteMapper;

        #endregion

        #region Initialization

        public SiteConfigurationCache(IDomainModelProvider domainModelProvider, ISiteMapper siteMapper)
            : base(new[] { typeof(Interfaces.Data.Site) })
        {
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
            }

            Dictionary<string, object> properties = null;

            if (siteId.HasValue)
            {
                using (var domainModel = _domainModelProvider.Create(siteId.Value))
                {
                    var site = domainModel.GetCollection<Interfaces.Data.Site>().First(s => s.Id == siteId.Value);
                    if (!string.IsNullOrWhiteSpace(site.Properties))
                    {
                        properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(site.Properties);
                    }
                }
            }

            foreach (var property in typeof(ISiteConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object value;

                if (properties == null || !properties.TryGetValue(property.Name, out value))
                {
                    var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultValue == null)
                    {
                        continue;
                    }
                    value = defaultValue.Value;
                }

                GetType().GetProperty(property.Name).SetValue(this, Convert.ChangeType(value, property.PropertyType));
            }
        }

        #endregion

        public string SiteName { get; set; }

        public int RecordsPerPage { get; set; }

        public string NoReplyEmail { get; set; }

        public string NoReplyName { get; set; }

        public string DefaultUserGroup { get; set; }

        public int MaximumImageSize { get; set; }

        public int ThumbnailWidth { get; set; }

        public int ThumbnailHeight { get; set; }

        public bool HasUsers { get; set; }

        public bool HasNavigationBar { get; set; }

        public bool HasSearch { get; set; }

        public bool UseLanguages { get; set; }

        public bool UseCurrencies { get; set; }

        public string DefaultCronJobId { get; set; }

        public string AdminLoginPage { get; set; }

        public bool AdminNotifyNewUsers { get; set; }

        public bool AdminApproveNewUsers { get; set; }

        public string UserAgreement { get; set; }
    }
}
