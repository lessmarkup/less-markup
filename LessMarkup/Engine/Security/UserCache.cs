/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.User;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Security
{
    class UserCache : AbstractCacheHandler, IUserCache
    {
        private long _userId;
        private readonly IDomainModelProvider _domainModelProvider;
        private List<long> _groups;

        public bool IsRemoved { get; private set; }
        public bool IsAdministrator { get; private set; }
        public bool IsGlobalAdministrator { get; private set; }
        public IReadOnlyList<long> Groups { get { return _groups; } }
        public bool IsValidated { get; private set; }
        public string Email { get; private set; }
        public string Title { get; private set; }
        public bool IsBlocked { get; private set; }
        public DateTime? UnblockTime { get; private set; }
        public long? SiteId { get; private set; }
        public string Name { get; private set; }

        public UserCache(IDomainModelProvider domainModelProvider)
            : base(new[] { EntityType.User })
        {
            _domainModelProvider = domainModelProvider;
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (!objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            _userId = objectId.Value;
            SiteId = siteId;

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.GetCollection<User>().Where(u => u.UserId == _userId)
                    .Select(u => new
                    {
                        u.Name,
                        u.Email,
                        u.IsAdministrator,
                        u.SiteId,
                        u.Title,
                        u.IsValidated,
                        u.IsBlocked,
                        u.UnblockTime,
                        Groups = u.Groups.Select(g => g.UserGroupId)

                    }).FirstOrDefault();

                if (user == null)
                {
                    IsRemoved = true;
                    return;
                }

                IsAdministrator = user.IsAdministrator;
                IsGlobalAdministrator = IsAdministrator && !user.SiteId.HasValue;
                _groups = user.Groups.ToList();
                Email = user.Email;
                Title = user.Title;
                IsValidated = user.IsValidated;
                IsBlocked = user.IsBlocked;
                UnblockTime = user.UnblockTime;
                Name = user.Name;

                if (IsBlocked && UnblockTime.HasValue && UnblockTime.Value < DateTime.UtcNow)
                {
                    IsBlocked = false;
                }
            }
        }

        protected override bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.User && entityId == _userId;
        }
    }
}
