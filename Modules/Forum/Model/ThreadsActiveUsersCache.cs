using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class ThreadsActiveUsersCache : AbstractCacheHandler
    {
        public const int ActiveUserThresholdMinutes = 5;

        public class ActiveUser
        {
            public ActiveUser(long userId, string name)
            {
                UserId = userId;
                Name = name;
            }

            public long UserId { get; private set; }
            public string Name { get; private set; }
        }

        public class ThreadUsers
        {
            public long ThreadId { get; set; }
            public DateTime LastUpdate { get; set; }
            public List<ActiveUser> Users { get; set; }
        }

        private readonly Dictionary<long, ThreadUsers> _users = new Dictionary<long, ThreadUsers>();
        private readonly object _syncObject = new object();

        private readonly ILightDomainModelProvider _domainModelProvider;

        public ThreadsActiveUsersCache(ILightDomainModelProvider domainModelProvider) : base(new Type[0])
        {
            _domainModelProvider = domainModelProvider;
        }

        protected override void Initialize(long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }
        }

        class ThreadUser
        {
            public long ThreadId { get; set; }
            public long UserId { get; set; }
            public string Name { get; set; }
        }

        public Dictionary<long, List<ActiveUser>> GetThreadUsers(List<long> threadIds)
        {
            var foundUsers = new List<ThreadUsers>();
            var newUsers = new List<long>();

            var expired = DateTime.Now.AddMinutes(-ActiveUserThresholdMinutes);

            lock (_syncObject)
            {
                foreach (var threadId in threadIds)
                {
                    ThreadUsers users;
                    if (!_users.TryGetValue(threadId, out users))
                    {
                        newUsers.Add(threadId);
                        continue;
                    }
                    if (users.LastUpdate < expired)
                    {
                        newUsers.Add(threadId);
                        _users.Remove(threadId);
                        continue;
                    }
                    foundUsers.Add(users);
                }

                if (newUsers.Any())
                {
                    var lastSeenCheck = DateTime.UtcNow.AddMinutes(-ActiveUserThresholdMinutes);

                    using (var domainModel = _domainModelProvider.Create())
                    {
                        var queryText = string.Format("SELECT tu.ThreadId, tu.UserId, u.Name " +
                            "FROM (SELECT DISTINCT ThreadId, UserId FROM ThreadViews WHERE LastSeen > $ AND UserId IS NOT NULL AND ThreadId IN ({0})) tu " +
                            "JOIN Users u ON u.Id = tu.UserId", string.Join(",", newUsers));

                        foreach (var thread in domainModel
                            .Query().Execute<ThreadUser>(queryText, lastSeenCheck).GroupBy(tu => tu.ThreadId))
                        {
                            var users = new ThreadUsers
                            {
                                ThreadId = thread.Key,
                                LastUpdate = DateTime.Now,
                                Users = thread.Select(tu => new ActiveUser(tu.UserId, tu.Name)).ToList()
                            };

                            _users[thread.Key] = users;

                            foundUsers.Add(users);
                        }
                    }
                }

                return foundUsers.ToDictionary(k => k.ThreadId, v => v.Users.ToList());
            }
        }
    }
}
