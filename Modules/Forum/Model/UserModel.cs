using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.User;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class UserModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Profile { get; set; }
        public string Avatar { get; set; }
        public int Posts { get; set; }
        public List<UserPropertyModel> Properties { get; set; }

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

            Posts = user.Posts;

            Properties =
                user.Properties.Where(p => p.Type == UserPropertyType.Text || p.Type == UserPropertyType.Date)
                    .Select(p => new UserPropertyModel
                    {
                        Name = p.Name,
                        Value = p.Value
                    })
                    .ToList();
        }

        public static void FillUsersFromPosts(Dictionary<string, object> values, IDataCache dataCache, IDomainModel domainModel, List<long> postIds)
        {
            FillUsers(values, dataCache, domainModel.GetSiteCollection<Post>().Where(p => p.UserId.HasValue && postIds.Contains(p.PostId)).GroupBy(p => p.UserId).Select(p => p.Key.Value).ToArray());
        }

        public static void FillUsers(Dictionary<string, object> values, IDataCache dataCache, params long[] userIds)
        {
            if (userIds == null || userIds.Length == 0)
            {
                return;
            }

            var users = new List<UserModel>();

            var cache = dataCache.Get<PostStatisticsCache>();

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
