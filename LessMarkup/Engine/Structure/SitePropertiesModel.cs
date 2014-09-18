/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

namespace LessMarkup.Engine.Structure
{
    [RecordModel]
    public class SitePropertiesModel : ISiteConfiguration
    {
        private readonly ISiteMapper _siteMapper;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;

        public SitePropertiesModel(ISiteMapper siteMapper, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
        {
            _changeTracker = changeTracker;
            _siteMapper = siteMapper;
            _domainModelProvider = domainModelProvider;
        }

        void ICacheHandler.Initialize(long? siteId, long? objectId)
        { throw new NotImplementedException(); }

        bool ICacheHandler.Expires(int collectionId, long entityId, EntityChangeType changeType)
        { throw new NotImplementedException(); }

        Type[] ICacheHandler.HandledCollectionTypes { get { throw new NotImplementedException(); } }

        bool ICacheHandler.Expired { get { throw new NotImplementedException(); } }

        [InputField(InputFieldType.Text, MainModuleTextIds.SiteName, Required = true)]
        public string SiteName { get; private set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.RecordsPerPage)]
        public int RecordsPerPage { get; private set; }

        [InputField(InputFieldType.Email, MainModuleTextIds.EmailForNoReply)]
        public string NoReplyEmail { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.UserNameForNoReply)]
        public string NoReplyName { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.DefaultUserGroup)]
        public string DefaultUserGroup { get; private set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.MaximumFileSize)]
        public int MaximumFileSize { get; private set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.ThumbnailWidth)]
        public int ThumbnailWidth { get; private set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.ThumbnailHeight)]
        public int ThumbnailHeight { get; private set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.MaximumImageWidth)]
        public int MaximumImageWidth { get; private set; }

        [InputField(InputFieldType.Number, MainModuleTextIds.MaximumImageHeight)]
        public int MaximumImageHeight { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasUsers)]
        public bool HasUsers { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasNavigationBar)]
        public bool HasNavigationBar { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.HasSearch)]
        public bool HasSearch { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.UseLanguages)]
        public bool UseLanguages { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.UseCurrencies)]
        public bool UseCurrencies { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.DefaultCronJobId)]
        public string DefaultCronJobId { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.AdminLoginPage)]
        public string AdminLoginPage { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.AdminNotifyNewUsers)]
        public bool AdminNotifyNewUsers { get; private set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.AdminApproveNewUsers)]
        public bool AdminApproveNewUsers { get; private set; }

        [InputField(InputFieldType.RichText, MainModuleTextIds.UserAgreement)]
        public string UserAgreement { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.GoogleAnalyticsResource)]
        public string GoogleAnalyticsResource { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.ValidFileType)]
        public string ValidFileType { get; private set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.ValidFileExtension)]
        public string ValidFileExtension { get; private set; }

        public void Initialize(long? siteId)
        {
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
                if (!siteId.HasValue)
                {
                    throw new ArgumentOutOfRangeException("siteId");
                }
            }

            Dictionary<string, object> properties = null;

            using (var domainModel = _domainModelProvider.Create())
            {
                var site = domainModel.GetCollection<Interfaces.Data.Site>().First(s => s.Id == siteId.Value);

                if (!string.IsNullOrWhiteSpace(site.Properties))
                {
                    properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(site.Properties);
                }
            }

            foreach (var property in typeof (ISiteConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object value;
                if (properties == null || !properties.TryGetValue(property.Name.ToJsonCase(), out value))
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

            var properties = new Dictionary<string, object>();
            foreach (var property in typeof(ISiteConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyValue = property.GetValue(this);

                var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

                if (defaultValue != null && defaultValue.Value == propertyValue)
                {
                    continue;
                }

                properties[property.Name.ToJsonCase()] = propertyValue;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var site = domainModel.GetCollection<Interfaces.Data.Site>().First(s => s.Id == siteId.Value);
                site.Properties = JsonConvert.SerializeObject(properties);
                _changeTracker.AddChange<Interfaces.Data.Site>(siteId.Value, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }
    }
}
