/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.DataObjects.User;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Configuration
{
    [RecordModel(CollectionType = typeof(CollectionManager))]
    public class NodeAccessModel
    {
        public class CollectionManager : IEditableModelCollection<NodeAccessModel>
        {
            private long _nodeId;
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
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
                _siteMapper = siteMapper;
            }

            public void Initialize(long? siteId, long nodeId)
            {
                _nodeId = nodeId;
                _siteId = siteId;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetSiteCollection<NodeAccess>(_siteId).Where(a => a.NodeId == _nodeId).Select(a => a.NodeAccessId);
            }

            public IQueryable<NodeAccessModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetSiteCollection<NodeAccess>(_siteId).Where(a => a.NodeId == _nodeId && ids.Contains(a.NodeAccessId)).Select(a => new NodeAccessModel
                {
                    AccessType = a.AccessType,
                    User = a.User.Email,
                    Group = a.Group.Name,
                    AccessId = a.NodeAccessId
                });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                _siteId = objectId;
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

                    var siteId = _siteId ?? _siteMapper.SiteId;

                    if (siteId.HasValue && !string.IsNullOrWhiteSpace(record.User))
                    {
                        access.UserId = domainModel.GetCollection<DataObjects.User.User>().Single(u => u.SiteId == _siteId.Value && u.Email == record.User).UserId;
                    }

                    if (siteId.HasValue && !string.IsNullOrWhiteSpace(record.Group))
                    {
                        access.GroupId = domainModel.GetSiteCollection<UserGroup>(_siteId).Single(g => g.Name == record.Group).UserGroupId;
                    }

                    domainModel.GetSiteCollection<NodeAccess>(_siteId).Add(access);
                    _changeTracker.AddChange(SiteId, EntityType.Site, EntityChangeType.Updated, domainModel);
                    _changeTracker.AddChange(_nodeId, EntityType.Node, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    record.AccessId = access.NodeAccessId;
                }
            }

            public void UpdateRecord(NodeAccessModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var access = domainModel.GetSiteCollection<NodeAccess>(_siteId).Single(a => a.NodeAccessId == record.AccessId);
                    access.AccessType = record.AccessType;

                    var siteId = _siteId ?? _siteMapper.SiteId;

                    if (siteId.HasValue && !string.IsNullOrWhiteSpace(record.User))
                    {
                        access.UserId = domainModel.GetCollection<DataObjects.User.User>().Single(u => u.SiteId == _siteId.Value && u.Email == record.User).UserId;
                    }
                    else
                    {
                        access.UserId = null;
                    }

                    if (siteId.HasValue && !string.IsNullOrWhiteSpace(record.Group))
                    {
                        access.GroupId = domainModel.GetSiteCollection<UserGroup>(_siteId).Single(g => g.Name == record.Group).UserGroupId;
                    }
                    else
                    {
                        access.GroupId = null;
                    }

                    _changeTracker.AddChange(SiteId, EntityType.Site, EntityChangeType.Updated, domainModel);
                    _changeTracker.AddChange(_nodeId, EntityType.Node, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var hasChanges = false;

                    foreach (var record in domainModel.GetSiteCollection<NodeAccess>(_siteId).Where(a => recordIds.Contains(a.NodeAccessId)))
                    {
                        domainModel.GetSiteCollection<NodeAccess>(_siteId).Remove(record);
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        _changeTracker.AddChange(SiteId, EntityType.Site, EntityChangeType.Updated, domainModel);
                        _changeTracker.AddChange(_nodeId, EntityType.Node, EntityChangeType.Updated, domainModel);
                        domainModel.SaveChanges();
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
