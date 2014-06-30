/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.User;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Framework.Security
{
    class UserCache : ICacheHandler
    {
        private long _userId;
        private readonly IDomainModelProvider _domainModelProvider;

        public bool IsRemoved { get; private set; }
        public bool IsAdministrator { get; private set; }
        public bool IsGlobalAdministrator { get; private set; }
        public List<long> Groups { get; private set; }
        public bool IsValidated { get; private set; }
        public string Email { get; private set; }
        public string Title { get; private set; }
        public bool IsBlocked { get; private set; }
        public DateTime? UnblockTime { get; private set; }
        public long? SiteId { get; private set; }

        public UserCache(IDomainModelProvider domainModelProvider)
        {
            _domainModelProvider = domainModelProvider;
        }

        public void Initialize(out DateTime? expirationTime, long? objectId = null)
        {
            if (!objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            _userId = objectId.Value;
            expirationTime = DateTime.UtcNow.AddMinutes(20);

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
                Groups = user.Groups.ToList();
                Email = user.Email;
                Title = user.Title;
                IsValidated = user.IsValidated;
                IsBlocked = user.IsBlocked;
                UnblockTime = user.UnblockTime;
                SiteId = user.SiteId;

                if (IsBlocked && UnblockTime.HasValue && UnblockTime.Value < DateTime.UtcNow)
                {
                    IsBlocked = false;
                }
            }
        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            if (entityType == EntityType.User && entityId == _userId)
            {
                return true;
            }

            return false;
        }

        private readonly EntityType[] _handledTypes = { EntityType.User };

        public EntityType[] HandledTypes { get { return _handledTypes; } }
    }
}
