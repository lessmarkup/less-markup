/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using Newtonsoft.Json;

namespace LessMarkup.Forum.Model
{
    public class UserModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Profile { get; set; }
        public string Avatar { get; set; }
        public int Posts { get; set; }
        public string Signature { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public void Initialize(PostStatisticsCache cache, long userId)
        {
            var user = cache.Get(userId);

            if (user == null || user.Removed)
            {
                Name = LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.UserRemoved);
                return;
            }

            Id = userId;
            Name = user.Name;
            Profile = UserHelper.GetUserProfileLink(user.UserId);
            if (user.AvatarId.HasValue)
            {
                Avatar = ImageHelper.ThumbnailUrl(user.AvatarId.Value);
            }

            Signature = user.Signature;
            Posts = user.Posts;
            Properties = !string.IsNullOrWhiteSpace(user.Properties) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(user.Properties) : new Dictionary<string, object>();
        }

        public static void FillUsersFromPosts(Dictionary<string, object> values, IDataCache dataCache, IDomainModel domainModel, List<long> postIds)
        {
            FillUsers(values, dataCache, domainModel.GetSiteCollection<Post>().Where(p => p.UserId.HasValue && postIds.Contains(p.Id)).GroupBy(p => p.UserId).Select(p => p.Key.Value).ToArray());
        }

        public static void FillUsers(Dictionary<string, object> values, IDataCache dataCache, params long[] userIds)
        {
            if (userIds == null || userIds.Length == 0)
            {
                return;
            }

            var users = new List<UserModel>();

            var cache = dataCache.Get<PostStatisticsCache>();

            cache.ReadUsers(userIds);

            foreach (var userId in userIds)
            {
                var user = new UserModel();
                user.Initialize(cache, userId);
                users.Add(user);
            }

            values["users"] = users;
        }
    }
}
