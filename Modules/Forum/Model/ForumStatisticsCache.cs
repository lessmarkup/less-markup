using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class ForumStatisticsCache : AbstractCacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;

        public ForumStatisticsCache(IDomainModelProvider domainModelProvider) : base(new[] { typeof(Thread)})
        {
            _domainModelProvider = domainModelProvider;
        }

        public int Posts { get; private set; }
        public int Threads { get; private set; }
        public string LastAuthor { get; set; }
        public long? LastAuthorId;
        public DateTime? LastCreated;
        public long? LastPostId { get; set; }
        public long? LastThreadId { get; set; }
        public string LastThreadPath { get; set; }
        public string LastThreadTitle { get; set; }

        private List<long> _threadIds;
        private long _forumId;

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (!objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            _forumId = objectId.Value;

            using (var domainModel = _domainModelProvider.Create())
            {
                var statistics = domainModel.GetSiteCollection<Post>().Where(p => p.Thread.ForumId == _forumId)
                    .GroupBy(p => p.Thread.ForumId)
                    .Select(g => new
                    {
                        Posts = g.Count(p => !p.Removed),
                        Threads = g.GroupBy(p => p.ThreadId),
                        LastPost = g.OrderByDescending(p => p.Created).FirstOrDefault()
                    })
                    .Select(f => new
                    {
                        f.Posts,
                        Threads = f.Threads.Count(),
                        ThreadIds = f.Threads.Select(t => t.Key).ToList(),
                        LastAuthor = f.LastPost != null ? f.LastPost.User.Name : null,
                        LastAuthorId = f.LastPost != null ? f.LastPost.UserId : null,
                        LastCreated = f.LastPost != null ? f.LastPost.Created : (DateTime?)null,
                        LastPostId = f.LastPost != null ? f.LastPost.Id : (long?)null,
                        LastThreadId = f.LastPost != null ? f.LastPost.ThreadId : (long?)null,
                        LastThreadTitle = f.LastPost != null ? f.LastPost.Thread.Name : null,
                        LastThreadPath = f.LastPost != null ? f.LastPost.Thread.Path : null
                    }).FirstOrDefault();

                if (statistics == null)
                {
                    return;
                }

                Posts = statistics.Posts;
                Threads = statistics.Threads;
                _threadIds = statistics.ThreadIds;
                LastAuthor = statistics.LastAuthor;
                LastAuthorId = statistics.LastAuthorId;
                LastCreated = statistics.LastCreated;
                LastPostId = statistics.LastPostId;
                LastThreadId = statistics.LastThreadId;
                LastThreadTitle = statistics.LastThreadTitle;
                LastThreadPath = statistics.LastThreadPath;
            }
        }

        protected override bool Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            if (changeType == EntityChangeType.Added)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var forumId = domainModel.GetSiteCollection<Thread>().First(t => t.Id == entityId).ForumId;
                    return forumId == _forumId;
                }
            }

            return _threadIds != null && _threadIds.Contains(entityId);
        }
    }
}
