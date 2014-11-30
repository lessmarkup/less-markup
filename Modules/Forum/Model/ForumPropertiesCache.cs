using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class ForumPropertiesCache : AbstractCacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;

        public ForumPropertiesCache(IDomainModelProvider domainModelProvider) : base(new Type[0])
        {
            _domainModelProvider = domainModelProvider;
        }

        public class ForumProperties
        {
            public long ForumId { get; set; }
            public int Posts { get; set; }
            public int Threads { get; set; }
            public string LastAuthor { get; set; }
            public long? LastAuthorId { get; set; }
            public DateTime? LastCreated { get; set; }
            public long? LastPostId { get; set; }
            public long? LastThreadId { get; set; }
            public string LastThreadPath { get; set; }
            public string LastThreadTitle { get; set; }
            public DateTime ValidUntil { get; set; }
        }

        private readonly Dictionary<long, ForumProperties> _forumProperties = new Dictionary<long, ForumProperties>();
        private readonly object _forumPropertiesLock = new object();

        public List<ForumProperties> GetPropertiesForForums(List<long> forumIds)
        {
            var ret = new List<ForumProperties>();

            lock (_forumPropertiesLock)
            {
                var currentTime = DateTime.UtcNow;
                ret.AddRange(_forumProperties.Values.Where(s => forumIds.Contains(s.ForumId) && s.ValidUntil >= currentTime));
            }

            var foundIds = ret.Select(s => s.ForumId).ToList();

            var missingIds = forumIds.Where(id => !foundIds.Contains(id)).ToList();

            if (missingIds.Any())
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var queryText = string.Format(
                            "SELECT s.[ForumId], s.[Posts], s.[Threads], s.[LastCreated], u.[Name] [LastAuthor], u.[Id] [LastAuthorId], p1.[Id] [LastPostId], t1.[Id] [LastThreadId], " +
                            "t1.[Name] [LastThreadTitle], t1.[Path] [LastThreadPath] FROM (" +
                            "SELECT t.[ForumId], COUNT(p.Id) [Posts], COUNT(t.[Id]) [Threads], MAX(p.[Created]) [LastCreated] FROM [Posts] p JOIN [Threads] t ON t.[Id] = p.[ThreadId] " +
                            "WHERE t.[ForumId] IN ({0}) GROUP BY t.[ForumId]) s LEFT JOIN [Posts] p1 ON p1.[Created] = s.[LastCreated] LEFT JOIN [Threads] t1 ON p1.[ThreadId] = t1.[Id] " +
                            "LEFT JOIN [Users] u ON u.[Id] = p1.[UserId]",
                            string.Join(",", missingIds));

                    var properties = domainModel.Query().Execute<ForumProperties>(queryText);

                    var validUntil = DateTime.UtcNow.AddMinutes(5);

                    lock (_forumPropertiesLock)
                    {
                        foreach (var stat in properties)
                        {
                            _forumProperties[stat.ForumId] = stat;
                            stat.ValidUntil = validUntil;
                        }

                        ret.AddRange(properties);
                    }
                }
            }

            return ret;
        }

        protected override void Initialize(long? objectId)
        {
        }
    }
}
