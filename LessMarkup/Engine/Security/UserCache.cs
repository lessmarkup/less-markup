/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.DataObjects.Security;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Security
{
    class UserCache : AbstractCacheHandler, IUserCache
    {
        private long? _userId;
        private readonly int _userCollectionId;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private List<long> _groups;

        public bool IsRemoved { get; private set; }
        public bool IsAdministrator { get; private set; }
        public bool IsApproved { get; private set; }
        public bool EmailConfirmed { get; private set; }
        public IReadOnlyList<long> Groups { get { return _groups; } }
        public string Email { get; private set; }
        public string Title { get; private set; }
        public bool IsBlocked { get; private set; }
        public DateTime? UnblockTime { get; private set; }
        public long? SiteId { get; private set; }
        public string Properties { get; private set; }
        public long? AvatarImageId { get; set; }
        public long? UserImageId { get; set; }
        public string Name { get; private set; }
        public IReadOnlyList<Tuple<ICachedNodeInformation, NodeAccessType>> Nodes { get; private set; }

        public UserCache(IDomainModelProvider domainModelProvider, IDataCache dataCache)
            : base(new[] { typeof(User), typeof(Node), typeof(NodeAccess), typeof(SiteProperties) })
        {
            _domainModelProvider = domainModelProvider;
            _userCollectionId = DataHelper.GetCollectionId<User>();
            _dataCache = dataCache;
        }

        private void InitializeUser()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.Query().From<User>().Where("Id = $", _userId).FirstOrDefault<User>();

                if (user == null)
                {
                    _userId = null;
                    return;
                }

                IsAdministrator = user.IsAdministrator;
                _groups = domainModel.Query().From<UserGroupMembership>().Where("UserId = $", _userId).ToList<UserGroupMembership>("UserGroupId").Select(g => g.UserGroupId).ToList();
                Email = user.Email;
                Title = user.Title;
                EmailConfirmed = user.EmailConfirmed;
                IsBlocked = user.IsBlocked;
                IsApproved = user.IsApproved;
                UnblockTime = user.UnblockTime;
                Properties = user.Properties;
                Name = user.Name;
                AvatarImageId = user.AvatarImageId;
                UserImageId = user.UserImageId;

                if (IsBlocked && UnblockTime.HasValue && UnblockTime.Value < DateTime.UtcNow)
                {
                    IsBlocked = false;
                }
            }
        }

        protected override void Initialize(long? objectId)
        {
            _userId = objectId;

            if (_userId.HasValue)
            {
                InitializeUser();
            }

            if (IsBlocked || IsRemoved)
            {
                _userId = null;
            }

            Nodes = _dataCache.Get<INodeCache>().Nodes
                .Select(n => new {Node = n, Rights = n.CheckRights(this, _userId)})
                .Where(n => n.Rights != NodeAccessType.NoAccess)
                .Select(n => Tuple.Create(n.Node, n.Rights)).ToList();
        }

        protected override bool Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            return collectionId != _userCollectionId || entityId == _userId;
        }
    }
}
