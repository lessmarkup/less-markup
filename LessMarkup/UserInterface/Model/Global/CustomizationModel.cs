/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(CollectionManager), TitleTextId = UserInterfaceTextIds.EditCustomization)]
    public class CustomizationModel
    {
        public class CollectionManager : IEditableModelCollection<CustomizationModel>
        {
            private long? _siteId;

            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public CollectionManager(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public void Initialize(long? siteId)
            {
                _siteId = siteId;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return
                    domainModel.GetSiteCollection<SiteCustomization>(_siteId)
                        .Select(c => c.SiteCustomizationId);
            }

            public IQueryable<CustomizationModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<SiteCustomization>(_siteId)
                        .Where(c => ids.Contains(c.SiteCustomizationId))
                        .Select(c => new CustomizationModel
                        {
                            Id = c.SiteCustomizationId,
                            Path = c.Path,
                            Type = c.Type,
                            Body = c.Body,
                            TypeDefined = true
                        });
            }

            public bool Filtered { get { return false; } }
            public CustomizationModel AddRecord(CustomizationModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var customization = new SiteCustomization
                    {
                        Created = DateTime.UtcNow,
                        Path = record.Path,
                        Type = record.Type,
                        Body = record.Body,
                    };

                    domainModel.GetSiteCollection<SiteCustomization>(_siteId).Add(customization);
                    domainModel.SaveChanges();
                    _changeTracker.AddChange(customization.SiteCustomizationId, EntityType.SiteCustomization, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    if (!returnObject)
                    {
                        return null;
                    }

                    record.Body = null;
                    record.Id = customization.SiteCustomizationId;

                    return record;
                }
            }

            public CustomizationModel UpdateRecord(CustomizationModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var customization = domainModel.GetSiteCollection<SiteCustomization>(_siteId).Single(c => c.SiteCustomizationId == record.Id);
                    customization.Path = record.Path;
                    if (record.Body != null)
                    {
                        customization.Body = record.Body;
                    }
                    _changeTracker.AddChange(record.Id, EntityType.SiteCustomization, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    if (!returnObject)
                    {
                        return null;
                    }

                    record.Body = null;

                    return record;
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var customization in domainModel.GetSiteCollection<SiteCustomization>(_siteId).Where(c => recordIds.Contains(c.SiteCustomizationId)))
                    {
                        domainModel.GetSiteCollection<SiteCustomization>(_siteId).Remove(customization);
                        _changeTracker.AddChange(customization.SiteCustomizationId, EntityType.SiteCustomization, EntityChangeType.Removed, domainModel);
                    }

                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    return true;
                }
            }
        }

        [InputField(InputFieldType.Hidden, DefaultValue = false)]
        public bool TypeDefined { get; set; }

        public long Id { get; set; }

        [JsonIgnore]
        public byte[] Body { get; set; }
        
        [InputField(InputFieldType.Select, UserInterfaceTextIds.CustomizationType, VisibleCondition = "!TypeDefined")]
        [Column(UserInterfaceTextIds.CustomizationType)]
        public SiteCustomizationType Type { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Path, Required = true)]
        [Column(UserInterfaceTextIds.Path)]
        public string Path { get; set; }

        [InputField(InputFieldType.File, UserInterfaceTextIds.File, VisibleCondition = "Type=='Image' || Type=='Binary'", Required = true)]
        public byte[] File
        {
            get
            {
                if (Type != SiteCustomizationType.Image)
                {
                    return null;
                }
                return Body;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    Body = value;
                }
            }
        }

        [InputField(InputFieldType.CodeText, UserInterfaceTextIds.Text, VisibleCondition = "Type!='Image' && Type!='Binary'", Required = true)]
        public string Text
        {
            get
            {
                if (Type == SiteCustomizationType.Image || Body == null)
                {
                    return null;
                }
                return Encoding.UTF8.GetString(Body);
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Body = Encoding.UTF8.GetBytes(value);
                }
            }
        }
    }
}
