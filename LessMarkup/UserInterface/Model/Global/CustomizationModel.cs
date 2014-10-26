/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
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
            private readonly ISiteMapper _siteMapper;

            private long SiteId
            {
                get
                {
                    var ret = _siteId ?? _siteMapper.SiteId;
                    if (!ret.HasValue)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    return ret.Value;
                }
            }


            public CollectionManager(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ISiteMapper siteMapper)
            {
                _siteMapper = siteMapper;
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return domainModel.GetSiteCollection<SiteCustomization>(SiteId).Select(c => c.Id);
            }

            public int CollectionId { get { return AbstractDomainModel.GetCollectionIdVerified<SiteCustomization>(); } }

            public IQueryable<CustomizationModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<SiteCustomization>(SiteId)
                        .Where(c => ids.Contains(c.Id))
                        .Select(c => new CustomizationModel
                        {
                            Id = c.Id,
                            Path = c.Path,
                            IsBinary = c.IsBinary,
                            Body = c.Body,
                            Append = c.Append,
                            TypeDefined = true
                        });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                _siteId = objectId;
            }

            public CustomizationModel CreateRecord()
            {
                return new CustomizationModel();
            }

            public void AddRecord(CustomizationModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var customization = new SiteCustomization
                    {
                        Created = DateTime.UtcNow,
                        Path = record.Path,
                        IsBinary = record.IsBinary,
                        Body = record.Body,
                        Append = record.Append
                    };

                    domainModel.GetSiteCollection<SiteCustomization>(SiteId).Add(customization);
                    domainModel.SaveChanges();
                    _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                    _changeTracker.AddChange(customization, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    record.Id = customization.Id;
                }
            }

            public void UpdateRecord(CustomizationModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var customization = domainModel.GetSiteCollection<SiteCustomization>(SiteId).Single(c => c.Id == record.Id);
                    customization.Path = record.Path;
                    customization.Append = record.Append;
                    if (record.Body != null)
                    {
                        customization.Body = record.Body;
                    }
                    _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                    _changeTracker.AddChange(customization, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var customization in domainModel.GetSiteCollection<SiteCustomization>(SiteId).Where(c => recordIds.Contains(c.Id)))
                    {
                        domainModel.GetSiteCollection<SiteCustomization>(SiteId).Remove(customization);
                        _changeTracker.AddChange(customization, EntityChangeType.Removed, domainModel);
                    }

                    _changeTracker.AddChange<Site>(SiteId, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    return true;
                }
            }

            public bool DeleteOnly { get { return false; } }
        }

        [InputField(InputFieldType.Hidden, DefaultValue = false)]
        public bool TypeDefined { get; set; }

        public long Id { get; set; }

        [JsonIgnore]
        public byte[] Body { get; set; }
        
        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.IsBinary, VisibleCondition = "!typeDefined")]
        [Column(UserInterfaceTextIds.IsBinary)]
        public bool IsBinary { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Path, Required = true)]
        [Column(UserInterfaceTextIds.Path)]
        public string Path { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.Append, DefaultValue = false, VisibleCondition = "!isBinary")]
        public bool Append { get; set; }

        [InputField(InputFieldType.File, UserInterfaceTextIds.File, VisibleCondition = "isBinary", Required = true)]
        public InputFile File
        {
            get
            {
                if (!IsBinary)
                {
                    return null;
                }
                return new InputFile
                {
                    File = Body,
                    Name = "File.bin",
                    Type = "binary"
                };
            }
            set
            {
                if (value != null && value.File != null && value.File.Length > 0)
                {
                    Body = value.File;
                }
            }
        }

        [InputField(InputFieldType.CodeText, UserInterfaceTextIds.Text, VisibleCondition = "!isBinary", Required = true)]
        public string Text
        {
            get
            {
                if (IsBinary || Body == null)
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
