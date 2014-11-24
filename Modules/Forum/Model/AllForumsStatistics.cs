using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
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
        private readonly ICurrentUser _currentUser;
        private readonly List<List<ForumStatisticsModel>> _groups = new List<List<ForumStatisticsModel>>();

        public List<List<ForumStatisticsModel>> Groups { get { return _groups; } }

        public AllForumsStatistics(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        private void InitializeNode(ForumStatisticsModel parent, ICachedNodeInformation node, Type forumHandlerType)
        {
            var accessType = node.CheckRights(_currentUser);

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
                    Description = node.Description,
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
                    forum.Parent.LastNodeId = forum.LastNodeId;
                    forum.Parent.LastThreadUrl = forum.LastThreadUrl;
                }
            }
        }

        public void OrganizeGroups(bool rootNode)
        {
            if (_forumsFlat.Count == 0)
            {
                return;
            }

            if (!rootNode)
            {
                var forums = _forumsFlat.Where(f => f.Level == 0).ToList();

                if (forums.Count > 0)
                {
                    _groups.Add(forums);
                }

                return;
            }

            if (!_forumsFlat.Any(f => f.Level > 0))
            {
                _groups.Add(_forumsFlat);
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

            var forumCache = _dataCache.Get<ForumStatisticsCache>();

            foreach (var statistics in forumCache.GetStatistics(_idToForum.Keys.ToList()))
            {
                var forum = _idToForum[statistics.ForumId];

                forum.Posts = statistics.Posts;
                forum.Threads = statistics.Threads;
                forum.LastAuthorId = statistics.LastAuthorId;
                forum.LastAuthorName = statistics.LastAuthor;
                forum.LastCreated = statistics.LastCreated;
                forum.LastPostId = statistics.LastPostId;
                forum.LastThreadId = statistics.LastThreadId;
                forum.LastThreadTitle = statistics.LastThreadTitle;

                if (statistics.LastPostId.HasValue)
                {
                    forum.LastNodeId = forum.Id;
                    forum.LastThreadUrl = string.Format("{0}/{1}", forum.Path, statistics.LastThreadPath);
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
