/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.DataFramework.Light;
using LessMarkup.DataObjects.Security;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.Model.Configuration
{
    [RecordModel(CollectionType = typeof(CollectionManager))]
    public class NodeAccessModel
    {
        public class CollectionManager : IEditableModelCollection<NodeAccessModel>
        {
            private long _nodeId;

            private readonly ILightDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public CollectionManager(ILightDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public void Initialize(long nodeId)
            {
                _nodeId = nodeId;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.From<NodeAccess>().Where("NodeId = $", _nodeId).ToIdList();
            }

            public int CollectionId { get { return LightDomainModel.GetCollectionId<NodeAccess>(); } }

            public IReadOnlyCollection<NodeAccessModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<NodeAccess>("na").Where(string.Format("na.NodeId = $ AND na.Id IN ({0})", string.Join(",", ids)), _nodeId)
                    .LeftJoin<DataObjects.Security.User>("u", "u.Id = na.UserId")
                    .LeftJoin<UserGroup>("g", "g.Id = na.GroupId")
                    .ToList<NodeAccessModel>("na.AccessType, u.Email, g.Name, na.Id AccessId");
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public NodeAccessModel CreateRecord()
            {
                return new NodeAccessModel();
            }

            public void AddRecord(NodeAccessModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var access = new NodeAccess
                    {
                        AccessType = record.AccessType,
                        NodeId = _nodeId,
                    };

                    if (!string.IsNullOrWhiteSpace(record.User))
                    {
                        access.UserId = domainModel.Query().From<DataObjects.Security.User>().Where("Email = $", record.User).First<DataObjects.Security.User>("Id").Id;
                    }

                    if (!string.IsNullOrWhiteSpace(record.Group))
                    {
                        access.GroupId = domainModel.Query().From<UserGroup>().Where("Name = $", record.Group).First<UserGroup>("Id").Id;
                    }

                    domainModel.Create(access);
                    _changeTracker.AddChange<Node>(_nodeId, EntityChangeType.Updated, domainModel);
                    domainModel.CompleteTransaction();

                    record.AccessId = access.Id;
                }
            }

            public void UpdateRecord(NodeAccessModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var access = domainModel.Query().Find<NodeAccess>(record.AccessId);
                    access.AccessType = record.AccessType;

                    if (!string.IsNullOrWhiteSpace(record.User))
                    {
                        access.UserId = domainModel.Query().From<DataObjects.Security.User>().Where("Email = $", record.User).First<DataObjects.Security.User>().Id;
                    }
                    else
                    {
                        access.UserId = null;
                    }

                    if (!string.IsNullOrWhiteSpace(record.Group))
                    {
                        access.GroupId = domainModel.Query().From<UserGroup>().Where("Name = $", record.Group).First<UserGroup>().Id;
                    }
                    else
                    {
                        access.GroupId = null;
                    }

                    domainModel.Update(access);
                    _changeTracker.AddChange<Node>(_nodeId, EntityChangeType.Updated, domainModel);
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var hasChanges = false;

                    foreach (var recordId in recordIds)
                    {
                        domainModel.Delete<NodeAccess>(recordId);
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        _changeTracker.AddChange<Node>(_nodeId, EntityChangeType.Updated, domainModel);
                        domainModel.CompleteTransaction();
                    }

                    return hasChanges;
                }
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long AccessId { get; set; }

        [InputField(InputFieldType.Select, UserInterfaceTextIds.AccessType, DefaultValue = NodeAccessType.Read)]
        [Column(UserInterfaceTextIds.AccessType)]
        public NodeAccessType AccessType { get; set; }

        [InputField(InputFieldType.Typeahead, UserInterfaceTextIds.User)]
        [Column(UserInterfaceTextIds.User)]
        public string User { get; set; }

        [InputField(InputFieldType.Typeahead, UserInterfaceTextIds.Group)]
        [Column(UserInterfaceTextIds.Group)]
        public string Group { get; set; }
    }
}
