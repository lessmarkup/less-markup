using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Forum.Model
{
    public class AllForumsStatistics
    {
        private readonly Dictionary<long, ForumStatisticsModel> _idToForum = new Dictionary<long, ForumStatisticsModel>();
        private readonly List<ForumStatisticsModel> _forums = new List<ForumStatisticsModel>();
        private readonly List<ForumStatisticsModel> _forumsFlat = new List<ForumStatisticsModel>();

        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ICurrentUser _currentUser;
        private readonly List<List<ForumStatisticsModel>> _groups = new List<List<ForumStatisticsModel>>();

        public List<List<ForumStatisticsModel>> Groups { get { return _groups; } }

        public AllForumsStatistics(IDataCache dataCache, IDomainModelProvider domainModelProvider, ICurrentUser currentUser)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        private void InitializeNode(ForumStatisticsModel parent, ICachedNodeInformation node, Type forumHandlerType)
        {
            var accessType = node.CheckRights(_currentUser) ?? NodeAccessType.Read;

            if (accessType == NodeAccessType.NoAccess)
            {
                return;
            }

            if (forumHandlerType.IsAssignableFrom(node.HandlerType))
            {
                var statistics = new ForumStatisticsModel
                {
                    Id = node.NodeId,
                    Title = node.Title,
                    Parent = parent,
                    Path = node.FullPath,
                    Children = new List<ForumStatisticsModel>(),
                    Level = parent != null ? parent.Level+1 : 0
                };

                if (parent != null)
                {
                    parent.Children.Add(statistics);
                }
                else
                {
                    _forums.Add(statistics);
                }

                _forumsFlat.Add(statistics);
                _idToForum[node.NodeId] = statistics;

                parent = statistics;
            }

            foreach (var child in node.Children)
            {
                InitializeNode(parent, child, forumHandlerType);
            }
        }

        private void Summarize(ForumStatisticsModel forum)
        {
            foreach (var child in forum.Children)
            {
                Summarize(child);
            }

            if (forum.Parent != null)
            {
                forum.Parent.Posts += forum.Posts;

                if (forum.LastCreated.HasValue && (!forum.Parent.LastCreated.HasValue || forum.Parent.LastCreated.Value < forum.LastCreated.Value))
                {
                    forum.Parent.LastAuthorId = forum.LastAuthorId;
                    forum.Parent.LastAuthorName = forum.LastAuthorName;
                    forum.Parent.LastAuthorUrl = forum.LastAuthorUrl;
                    forum.Parent.LastCreated = forum.LastCreated;
                    forum.Parent.LastPostId = forum.LastPostId;
                    forum.Parent.LastThreadId = forum.LastThreadId;
                    forum.Parent.LastThreadTitle = forum.LastThreadTitle;
                }
            }
        }

        public void OrganizeGroups()
        {
            if (!_forumsFlat.Any(f => f.Level > 0))
            {
                if (_forumsFlat.Count > 0)
                {
                    _groups.Add(_forumsFlat);
                }
                return;
            }

            List<ForumStatisticsModel> group = null;

            foreach (var forum in _forumsFlat)
            {
                if (forum.Level == 0)
                {
                    forum.Children = null;

                    if (@group == null)
                    {
                        @group = new List<ForumStatisticsModel>();
                        _groups.Add(@group);
                        @group.Add(forum);
                        forum.Children = null;
                        continue;
                    }

                    if (@group.All(g => g.Level == 0))
                    {
                        @group.Add(forum);
                        continue;
                    }

                    @group = new List<ForumStatisticsModel>();
                    _groups.Add(@group);
                    @group.Add(forum);
                    continue;
                }

                if (@group == null)
                {
                    throw new Exception("Wrong level");
                }

                if (forum.Level != 1)
                {
                    continue;
                }

                @group.Add(forum);
                @group[0].IsHeader = true;
            }
        }

        public void CollectStatistics(long nodeId, Type forumHandlerType)
        {
            var nodeCache = _dataCache.Get<INodeCache>();

            var node = nodeCache.GetNode(nodeId);

            foreach (var child in node.Children)
            {
                InitializeNode(null, child, forumHandlerType);
            }

            var forumIds = _idToForum.Keys.ToList();

            using (var domainModel = _domainModelProvider.Create())
            {
                var statistics = domainModel.GetSiteCollection<Post>()
                    .Where(p => forumIds.Contains(p.Thread.ForumId))
                    .GroupBy(p => p.Thread.ForumId)
                    .Select(g => new
                    {
                        ForumId = g.Key,
                        Posts = g.Count(p => !p.Removed),
                        LastPost = g.OrderByDescending(p => p.Created).FirstOrDefault()
                    })
                    .Select(f => new
                    {
                        f.ForumId,
                        f.Posts,
                        LastAuthor = f.LastPost != null ? f.LastPost.User.Name : null,
                        LastAuthorId = f.LastPost != null ? f.LastPost.UserId : null,
                        LastCreated = f.LastPost != null ? f.LastPost.Created : (DateTime?)null,
                        LastPostId = f.LastPost != null ? f.LastPost.Id : (long?)null,
                        LastThreadId = f.LastPost != null ? f.LastPost.ThreadId : (long?)null,
                        LastThreadTitle = f.LastPost != null ? f.LastPost.Thread.Name : null
                    });

                foreach (var forum in statistics)
                {
                    var model = _idToForum[forum.ForumId];

                    model.Posts = forum.Posts;
                    model.LastAuthorId = forum.LastAuthorId;
                    model.LastAuthorName = forum.LastAuthor;
                    model.LastCreated = forum.LastCreated;
                    model.LastPostId = forum.LastPostId;
                    model.LastThreadId = forum.LastThreadId;
                    model.LastThreadTitle = forum.LastThreadTitle;
                }
            }

            foreach (var forum in _idToForum.Values)
            {
                if (forum.LastAuthorId.HasValue)
                {
                    forum.LastAuthorUrl = UserHelper.GetUserProfileLink(forum.LastAuthorId.Value);
                }
            }

            foreach (var forum in _forums)
            {
                Summarize(forum);
            }
        }
    }
}
