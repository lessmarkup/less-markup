/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(Collection))]
    public class UserGroupModel
    {
        public class Collection : IEditableModelCollection<UserGroupModel>
        {
            private long? _siteId;

            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;
            private readonly ISiteMapper _siteMapper;

            private long SiteId
            {
                get
                {
                    var siteId = _siteId ?? _siteMapper.SiteId;

                    if (siteId.HasValue)
                    {
                        return siteId.Value;
                    }

                    throw new Exception("Unknown site");
                }
            }

            public Collection(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ISiteMapper siteMapper)
            {
                _changeTracker = changeTracker;
                _domainModelProvider = domainModelProvider;
                _siteMapper = siteMapper;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return domainModel.GetSiteCollection<UserGroup>(SiteId).Select(g => g.Id);
            }

            public int CollectionId { get { return AbstractDomainModel.GetCollectionIdVerified<UserGroup>(); } }

            public IQueryable<UserGroupModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<UserGroup>(SiteId)
                        .Where(g => ids.Contains(g.Id))
                        .Select(g => new UserGroupModel
                        {
                            GroupId = g.Id,
                            Name = g.Name,
                            Description = g.Description
                        });
            }

            public bool Filtered { get { return false; } }
            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                _siteId = objectId;
            }

            public UserGroupModel CreateRecord()
            {
                return new UserGroupModel();
            }

            public void AddRecord(UserGroupModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var group = new UserGroup
                    {
                        Name = record.Name,
                        Description = record.Description,
                    };

                    domainModel.GetSiteCollection<UserGroup>(SiteId).Add(group);
                    domainModel.SaveChanges();
                    _changeTracker.AddChange(group, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    record.GroupId = group.Id;
                }
            }

            public void UpdateRecord(UserGroupModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var group = domainModel.GetSiteCollection<UserGroup>(SiteId).Single(g => g.Id == record.GroupId);

                    group.Name = record.Name;
                    group.Description = record.Description;

                    _changeTracker.AddChange(group, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var group in domainModel.GetSiteCollection<UserGroup>(SiteId).Where(g => recordIds.Contains(g.Id)))
                    {
                        domainModel.GetSiteCollection<UserGroup>(SiteId).Remove(group);
                        _changeTracker.AddChange(group, EntityChangeType.Removed, domainModel);
                    }

                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    return true;
                }
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long GroupId { get; set; }

        [Column(UserInterfaceTextIds.Name)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.Name, Required = true)]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Description)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.Description)]
        public string Description { get; set; }
    }
}
