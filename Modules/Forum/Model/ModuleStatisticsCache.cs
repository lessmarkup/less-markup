using System;
using System.Collections.Generic;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class ModuleStatisticsCache : AbstractCacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private DateTime _expires;

        private List<UserStatistics> _activeUsers;
        private List<ForumStatistics> _statistics;

        public ModuleStatisticsCache(IDomainModelProvider domainModelProvider) : base(new[] { typeof(DataObjects.Thread), typeof(Node) })
        {
            _domainModelProvider = domainModelProvider;
        }

        public class ForumStatistics
        {
            public int Id { get; set; }
            public int Value { get; set; }

            public string Name
            {
                get
                {
                    switch (Id)
                    {
                        case 1:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.AllPosts);
                        case 2:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.AllThreads);
                        case 3:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.ThreadsToday);
                        case 4:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.ThreadsYesterday);
                        case 5:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.PostsToday);
                        case 6:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.PostsYesterday);
                        case 7:
                            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.AllUsers);
                        default:
                            throw new ArgumentException("Id");
                    }
                }
            }
        }

        public class UserStatistics
        {
            public string Name { get; set; }
            public long Id { get; set; }
            public string Url { get; set; }
        }

        protected override void Initialize(long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var dateThreshold = DateTime.UtcNow.AddMinutes(-10);

                _activeUsers = domainModel.Query()
                    .Execute<UserStatistics>("SELECT t.UserId, u.Name, t.LastSeen FROM (SELECT MAX(LastSeen) LastSeen, UserId FROM ThreadViews WHERE LastSeen > $ AND UserId IS NOT NULL GROUP BY UserId) t JOIN Users u ON u.Id = t.UserId ORDER BY t.LastSeen DESC", dateThreshold);

                foreach (var user in _activeUsers)
                {
                    user.Url = UserHelper.GetUserProfileLink(user.Id);
                }

                _statistics = domainModel.Query().Execute<ForumStatistics>("SELECT COUNT(p.Id) [Value], 1 [Id] FROM Posts p WHERE p.Removed = 0 " +
                    "UNION SELECT COUNT(t.Id) [Value], 2 [Id] FROM Threads t WHERE t.Removed = 0 " +
                    "UNION SELECT COUNT(t.Id) [Value], 3 [Id] FROM Threads t WHERE t.Removed = 0 AND t.Created >= $0 " +
                    "UNION SELECT COUNT(t.Id) [Value], 4 [Id] FROM Threads t WHERE t.Removed = 0 AND t.Created >= $1 AND t.Created < $0 " +
                    "UNION SELECT COUNT(p.Id) [Value], 5 [Id] FROM Posts p WHERE p.Removed = 0 AND p.Created >= $0 " +
                    "UNION SELECT COUNT(p.Id) [Value], 6 [Id] FROM Posts p WHERE p.Removed = 0 AND p.Created >= $1 AND p.Created < $0 " +
                    "UNION SELECT COUNT(u.Id) [Value], 7 [Id] FROM Users u WHERE u.IsRemoved = 0", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(-1));
            }

            _expires = DateTime.Now.AddMinutes(3);
        }

        public List<UserStatistics> ActiveUsers { get { return _activeUsers; } }
        public List<ForumStatistics> Forums { get { return _statistics; } } 

        protected override bool Expired
        {
            get { return DateTime.Now > _expires; }
        }
    }
}
