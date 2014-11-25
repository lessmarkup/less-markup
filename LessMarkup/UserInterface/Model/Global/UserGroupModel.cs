/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.Light;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(Collection), DataType = typeof(UserGroup))]
    public class UserGroupModel
    {
        public class Collection : IEditableModelCollection<UserGroupModel>
        {
            private readonly ILightDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public Collection(ILightDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _changeTracker = changeTracker;
                _domainModelProvider = domainModelProvider;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.From<UserGroup>().ToIdList();
            }

            public int CollectionId { get { return LightDomainModel.GetCollectionId<UserGroup>(); } }

            public IReadOnlyCollection<UserGroupModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<UserGroup>().WhereIds(ids).ToList<UserGroup>()
                        .Select(g => new UserGroupModel
                        {
                            GroupId = g.Id,
                            Name = g.Name,
                            Description = g.Description
                        }).ToList();
            }

            public bool Filtered { get { return false; } }
            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
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

                    domainModel.Create(group);
                    _changeTracker.AddChange(group, EntityChangeType.Added, domainModel);
                    domainModel.CompleteTransaction();

                    record.GroupId = group.Id;
                }
            }

            public void UpdateRecord(UserGroupModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var group = domainModel.Query().Find<UserGroup>(record.GroupId);

                    group.Name = record.Name;
                    group.Description = record.Description;

                    _changeTracker.AddChange(group, EntityChangeType.Updated, domainModel);
                    domainModel.Update(group);
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var id in recordIds)
                    {
                        domainModel.Delete<UserGroup>(id);
                    }

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
