/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Configuration
{
    [RecordModel]
    public class SiteConfigurationCache : AbstractCacheHandler
    {
        #region Private Fields

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly ISiteMapper _siteMapper;

        #endregion

        #region Initialization

        public SiteConfigurationCache(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ISiteMapper siteMapper)
            : base(new[] { EntityType.Site })
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _siteMapper = siteMapper;
        }

        void InitializeFromProperties(Dictionary<string, SiteProperty> existingProperties)
        {
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (existingProperties == null || !existingProperties.ContainsKey(property.Name))
                {
                    var inputFieldAttribute = property.GetCustomAttribute<InputFieldAttribute>();
                    if (inputFieldAttribute == null)
                    {
                        continue;
                    }

                    if (inputFieldAttribute.DefaultValue != null)
                    {
                        property.SetValue(this, inputFieldAttribute.DefaultValue);
                    }
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

        public void Initialize(long? siteId)
        {
            Initialize(siteId, null);
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
            }

            if (!siteId.HasValue)
            {
                InitializeFromProperties(null);
                return;
            }

            using (var domainModel = _domainModelProvider.Create(siteId.Value))
            {
                var existingProperties = domainModel.GetCollection<SiteProperty>().Where(p => p.SiteId == siteId.Value).ToDictionary(p => p.Name);
                InitializeFromProperties(existingProperties);
            }
        }

        #endregion

        #region Public Methods

        public void Save(long? siteId)
        {
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
                if (!siteId.HasValue)
                {
                    throw new ArgumentOutOfRangeException("siteId");
                }
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

                    var propertyRecord = domainModel.GetCollection<SiteProperty>().FirstOrDefault(p => p.SiteId == siteId.Value && p.Name == property.Name);

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

        [InputField(InputFieldType.Text, MainModuleTextIds.SiteName, DefaultValue = "Site", Required = true)]
        public string SiteName { get; set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.RecordsPerPage, DefaultValue = 10)]
        public int RecordsPerPage { get; set; }

        [InputField(InputFieldType.Email, MainModuleTextIds.EmailForNoReply)]
        public string NoReplyEmail { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.UserNameForNoReply)]
        public string NoReplyName { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.DefaultUserGroup, DefaultValue = "Registered")]
        public string DefaultUserGroup { get; set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.MaximumImageSize, DefaultValue = 1024 * 1024 * 10)]
        public int MaximumImageSize { get; set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.ThumbnailWidth, DefaultValue = 75)]
        public int ThumbnailWidth { get; set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.ThumbnailHeight, DefaultValue = 75)]
        public int ThumbnailHeight { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasUsers, DefaultValue = false)]
        public bool HasUsers { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasNavigationBar, DefaultValue = false)]
        public bool HasNavigationBar { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasSearch, DefaultValue = false)]
        public bool HasSearch { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasLanguages, DefaultValue = false)]
        public bool HasLanguages { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasCurrencies, DefaultValue = false)]
        public bool HasCurrencies { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.DefaultCronJobId, DefaultValue = "Default")]
        public string DefaultCronJobId { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.AdminLoginPage)]
        public string AdminLoginPage { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.AdminNotifyNewUsers, DefaultValue = false)]
        public bool AdminNotifyNewUsers { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.AdminApproveNewUsers, DefaultValue = false)]
        public bool AdminApproveNewUsers { get; set; }

        [InputField(InputFieldType.RichText, MainModuleTextIds.UserAgreement)]
        public string UserAgreement { get; set; }
    }
}
