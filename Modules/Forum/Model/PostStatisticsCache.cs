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
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly Dictionary<long, int> _userPosts = new Dictionary<long, int>();

        public PostStatisticsCache(IDomainModelProvider domainModelProvider)
            : base(new[] { EntityType.ForumPost })
        {
            _domainModelProvider = domainModelProvider;
        }

        public int GetPostCount(long userId)
        {
            int ret;
            if (!_userPosts.TryGetValue(userId, out ret))
            {
                return 0;
            }
            return ret;
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var user in domainModel.GetSiteCollection<Post>().Where(p => !p.Removed).GroupBy(p => p.UserId).Select(p => new { UserId = p.Key, Posts = p.Count() }))
                {
                    _userPosts[user.UserId.Value] = user.Posts;
                }
            }
        }
    }
}
