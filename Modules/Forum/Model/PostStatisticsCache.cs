/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
        }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly Dictionary<long, UserStatistics> _userPosts = new Dictionary<long, UserStatistics>();
        private readonly object _readLock = new object();

        public PostStatisticsCache(IDomainModelProvider domainModelProvider)
            : base(new[] { typeof(Post) })
        {
            _domainModelProvider = domainModelProvider;
        }

        public void ReadUsers(IEnumerable<long> userIds)
        {
            lock (_readLock)
            {
                var usersToFetch = userIds.Where(id => !_userPosts.ContainsKey(id)).ToList();

                if (usersToFetch.Count == 0)
                {
                    return;
                }

                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var user in domainModel.GetSiteCollection<Post>()
                        .Where(p => !p.Removed && p.UserId.HasValue && usersToFetch.Contains(p.UserId.Value))
                        .GroupBy(p => p.User).Select(p => new
                    {
                        p.Key.Id,
                        Posts = p.Count(),
                        AvatarId = p.Key.AvatarImageId,
                        p.Key.Name,
                        p.Key.IsRemoved,
                        p.Key.Properties,
                        p.Key.Signature,
                    }))
                    {
                        _userPosts[user.Id] = new UserStatistics
                        {
                            AvatarId = user.AvatarId,
                            Name = user.Name,
                            Posts = user.Posts,
                            Removed = user.IsRemoved,
                            UserId = user.Id,
                            Properties = user.Properties,
                            Signature = user.Signature
                        };
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

        protected override void Initialize(long? siteId, long? objectId)
        {
        }
    }
}
