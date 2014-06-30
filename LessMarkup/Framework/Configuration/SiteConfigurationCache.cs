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

namespace LessMarkup.Framework.Configuration
{
    public class SiteConfigurationCache : ICacheHandler
    {
        #region Private Fields

        private readonly EntityType[] _handledTypes = { EntityType.Site };
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISiteMapper _siteMapper;
        private readonly IChangeTracker _changeTracker;

        #endregion

        #region Initialization

        public SiteConfigurationCache(IDomainModelProvider domainModelProvider, ISiteMapper siteMapper, IChangeTracker changeTracker)
        {
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;
            _changeTracker = changeTracker;
        }

        void Initialize(Dictionary<string, SiteProperty> existingProperties)
        {
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (existingProperties == null || !existingProperties.ContainsKey(property.Name))
                {
                    var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultValue == null)
                    {
                        continue;
                    }

                    property.SetValue(this, defaultValue.Value);
                    continue;
                }

                var existingValue = existingProperties[property.Name].Value;

                if (property.PropertyType == typeof (int))
                {
                    int value;
                    if (int.TryParse(existingValue, out value))
                    {
                        property.SetValue(this, value);
                    }
                }
                else if (property.PropertyType == typeof (bool))
                {
                    bool value;
                    if (bool.TryParse(existingValue, out value))
                    {
                        property.SetValue(this, value);
                    }
                }
                else if (property.PropertyType == typeof (string))
                {
                    property.SetValue(this, existingValue);
                }
            }
        }

        void ICacheHandler.Initialize(out DateTime? expirationTime, long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            expirationTime = null;

            var siteId = _siteMapper.SiteId;
            if (!siteId.HasValue)
            {
                Initialize(null);
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var existingProperties = domainModel.GetCollection<SiteProperty>().Where(p => p.SiteId == siteId).ToDictionary(p => p.Name);
                Initialize(existingProperties);
            }
        }

        #endregion

        #region Public Methods

        public void Save()
        {
            var siteId = _siteMapper.SiteId;
            if (!siteId.HasValue)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

                    object propertyValue = property.GetValue(this);

                    if (defaultValue != null)
                    {
                        if (propertyValue != null && propertyValue.Equals(defaultValue.Value))
                        {
                            propertyValue = null;
                        }
                    }

                    var propertyRecord = domainModel.GetCollection<SiteProperty>().FirstOrDefault(p => p.SiteId == siteId && p.Name == property.Name);

                    if (propertyRecord == null)
                    {
                        if (propertyValue == null)
                        {
                            continue;
                        }

                        propertyRecord = new SiteProperty
                        {
                            Name = property.Name, 
                            SiteId = siteId.Value,
                            Value = propertyValue.ToString()
                        };
                        domainModel.GetCollection<SiteProperty>().Add(propertyRecord);
                    }
                    else
                    {
                        if (propertyValue == null)
                        {
                            domainModel.GetCollection<SiteProperty>().Remove(propertyRecord);
                        }
                        else
                        {
                            propertyRecord.Value = propertyValue.ToString();
                        }
                    }
                }

                _changeTracker.AddChange(siteId.Value, EntityType.Site, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }

        #endregion

        #region ICacheHandler Members

        bool ICacheHandler.Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.Site;
        }

        EntityType[] ICacheHandler.HandledTypes { get { return _handledTypes; } }

        #endregion

        [DefaultValue(10)]
        public int RecordsPerPage { get; set; }

        public string NoReplyEmail { get; set; }

        public string NoReplyName { get; set; }

        [DefaultValue("Registered")]
        public string DefaultUserGroup { get; set; }

        [DefaultValue("Your Site")]
        public string SiteName { get; set; }

        public string HomePageEntity { get; set; }

        [DefaultValue("Navigation Bar")]
        public string NavigationBar { get; set; }

        [DefaultValue(1024 * 1024 * 10)]
        public int MaximumImageSize { get; set; }

        [DefaultValue(40)]
        public int ThumbnailWidth { get; set; }

        [DefaultValue(40)]
        public int ThumbnailHeight { get; set; }

        [DefaultValue(180)]
        public int BoxWidth { get; set; }

        [DefaultValue(100)]
        public int BoxHeight { get; set; }

        [DefaultValue(100)]
        public int AvatarWidth { get; set; }

        [DefaultValue(100)]
        public int AvatarHeight { get; set; }

        [DefaultValue(500)]
        public int MaximumImageWidth { get; set; }

        [DefaultValue(600)]
        public int MaximumImageHeight { get; set; }

        [DefaultValue(false)]
        public bool HasUsers { get; set; }

        [DefaultValue(false)]
        public bool HasNavigationBar { get; set; }

        [DefaultValue(false)]
        public bool HasSearch { get; set; }

        [DefaultValue(false)]
        public bool HasLanguages { get; set; }

        [DefaultValue(false)]
        public bool HasCurrencies { get; set; }

        [DefaultValue("Default")]
        public string DefaultCronJobId { get; set; }

        [DefaultValue("")]
        public string AdminLoginPage { get; set; }

        [DefaultValue(false)]
        public bool AdminNotifyNewUsers { get; set; }

        [DefaultValue(false)]
        public bool AdminApproveNewUsers { get; set; }
    }
}
