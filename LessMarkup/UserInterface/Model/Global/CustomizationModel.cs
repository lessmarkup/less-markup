/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(CollectionManager), TitleTextId = UserInterfaceTextIds.EditCustomization)]
    public class CustomizationModel
    {
        public class CollectionManager : IEditableModelCollection<CustomizationModel>
        {
            private readonly ILightDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public CollectionManager(ILightDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.From<SiteCustomization>().ToIdList();
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<SiteCustomization>(); } }

            public IReadOnlyCollection<CustomizationModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<SiteCustomization>().ToList<SiteCustomization>()
                        .Select(c => new CustomizationModel
                        {
                            Id = c.Id,
                            Path = c.Path,
                            IsBinary = c.IsBinary,
                            Body = c.Body,
                            Append = c.Append,
                            TypeDefined = true
                        }).ToList();
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
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

                    domainModel.Create(customization);
                    _changeTracker.AddChange(customization, EntityChangeType.Added, domainModel);
                    domainModel.CompleteTransaction();
                    record.Id = customization.Id;
                }
            }

            public void UpdateRecord(CustomizationModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var customization = domainModel.Query().Find<SiteCustomization>(record.Id);
                    customization.Path = record.Path;
                    customization.Append = record.Append;
                    if (record.Body != null)
                    {
                        customization.Body = record.Body;
                    }
                    domainModel.Update(customization);
                    _changeTracker.AddChange(customization, EntityChangeType.Updated, domainModel);
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var id in recordIds)
                    {
                        domainModel.Delete<SiteCustomization>(id);
                        _changeTracker.AddChange<SiteCustomization>(id, EntityChangeType.Removed, domainModel);
                    }

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
