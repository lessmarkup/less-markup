using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
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

        private readonly IDomainModelProvider _domainModelProvider;

        public ThreadsActiveUsersCache(IDomainModelProvider domainModelProvider) : base(new Type[0])
        {
            _domainModelProvider = domainModelProvider;
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }
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
                        foreach (var thread in domainModel.GetSiteCollection<ThreadView>()
                            .Where(tv => newUsers.Contains(tv.ThreadId) && tv.LastSeen >= lastSeenCheck && tv.UserId.HasValue)
                            .Select(tv => new { tv.ThreadId, tv.User.Name, UserId = tv.UserId.Value })
                            .GroupBy(tv => tv.ThreadId))
                        {
                            var users = new ThreadUsers
                            {
                                ThreadId = thread.Key,
                                LastUpdate = DateTime.Now,
                                Users = thread.Distinct().Select(tu => new ActiveUser(tu.UserId, tu.Name)).ToList()
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
