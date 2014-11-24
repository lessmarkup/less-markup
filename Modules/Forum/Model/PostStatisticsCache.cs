/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class PostStatisticsCache : AbstractCacheHandler
    {
        public class UserStatistics
        {
            public string Name { get; set; }
            public int Posts { get; set; }
            public long? AvatarId { get; set; }
            public long UserId { get; set; }
            public bool Removed { get; set; }
            public string Properties { get; set; }
            public string Signature { get; set; }
            public DateTime ValidUntil { get; set; }
        }

        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly Dictionary<long, UserStatistics> _userPosts = new Dictionary<long, UserStatistics>();
        private readonly object _readLock = new object();

        public PostStatisticsCache(ILightDomainModelProvider domainModelProvider)
            : base(new[] { typeof(Post) })
        {
            _domainModelProvider = domainModelProvider;
        }

        public void ReadUsers(IEnumerable<long> userIds)
        {
            lock (_readLock)
            {
                var currentDate = DateTime.UtcNow;
                var usersToFetch = userIds.Where(id => !_userPosts.ContainsKey(id) || _userPosts[id].ValidUntil < currentDate).ToList();

                if (usersToFetch.Count == 0)
                {
                    return;
                }

                using (var domainModel = _domainModelProvider.Create())
                {
                    var idsText = string.Join(",", usersToFetch);
                    var query = domainModel.Query().Execute<UserStatistics>(
                                string.Format("SELECT u.[Id] [UserId], s.[Posts], u.[AvatarImageId] [AvatarId], u.[Name], u.[IsRemoved] [Removed], u.[Properties], u.[Signature] FROM (SELECT * FROM [Users] WHERE [Id] IN ({0})) u LEFT JOIN (SELECT p.[UserId], COUNT(p.[Id]) [Posts] FROM [Posts] p WHERE p.[UserId] IN ({0}) GROUP BY p.[UserId]) s ON s.[UserId] = u.[Id]", idsText));

                    var validUntil = DateTime.UtcNow.AddMinutes(5);

                    foreach (var user in query)
                    {
                        user.ValidUntil = validUntil;
                        _userPosts[user.UserId] = user;
                    }
                }
            }
        }

        public UserStatistics Get(long userId)
        {
            lock (_readLock)
            {
                UserStatistics ret;
                if (!_userPosts.TryGetValue(userId, out ret))
                {
                    return null;
                }
                return ret;
            }
        }

        protected override void Initialize(long? objectId)
        {
        }
    }
}
