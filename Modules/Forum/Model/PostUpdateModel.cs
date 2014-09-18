/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

namespace LessMarkup.Forum.Model
{
    [RecordModel(CollectionType = typeof(Collection))]
    public class PostUpdateModel
    {
        public class Collection : IModelCollection<PostUpdateModel>
        {
            private readonly IDataCache _dataCache;
            private readonly ICurrentUser _currentUser;
            private long _nodeId;

            public Collection(IDataCache dataCache, ICurrentUser currentUser)
            {
                _dataCache = dataCache;
                _currentUser = currentUser;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                var userId = _currentUser.UserId;

                List<long> selectedForumIds = null;

                if (userId.HasValue)
                {
                    var nodeUserData = domainModel.GetSiteCollection<NodeUserData>().FirstOrDefault(d => d.NodeId == _nodeId && d.UserId == userId.Value);
                    if (nodeUserData != null && !string.IsNullOrEmpty(nodeUserData.Settings))
                    {
                        var nodeSettings = JsonConvert.DeserializeObject<PostUpdatesUserSettingsModel>(nodeUserData.Settings);

                        if (nodeSettings.ForumIds != null && nodeSettings.ForumIds.Count > 0)
                        {
                            selectedForumIds = nodeSettings.ForumIds;
                        }
                    }
                    if (selectedForumIds == null || selectedForumIds.Count == 0)
                    {
                        return new EnumerableQuery<long>(new long[0]);
                    }
                }

                var nodeCache = _dataCache.Get<INodeCache>();
                var collectionId = DataHelper.GetCollectionId<Post>();
                if (!collectionId.HasValue)
                {
                    return new EnumerableQuery<long>(new long[0]);
                }

                var changesCache = _dataCache.Get<IChangesCache>();
                var changesQuery = userId.HasValue ? 
                    changesCache.GetCollectionChanges(collectionId.Value, null, null, f => f.Type != EntityChangeType.Removed && f.UserId != userId) : 
                    changesCache.GetCollectionChanges(collectionId.Value, null, null, f => f.Type != EntityChangeType.Removed);
                if (changesQuery == null)
                {
                    return new EnumerableQuery<long>(new long[0]);
                }
                var changesIds = changesQuery.Select(c => c.EntityId).ToList();
                var nodeIds = nodeCache.Nodes.Where(n => n.CheckRights(_currentUser) != NodeAccessType.NoAccess ).Select(n => n.NodeId).ToList();

                var query = domainModel.GetSiteCollection<Post>().Where(p => !p.Removed && nodeIds.Contains(p.Thread.ForumId) && changesIds.Contains(p.Id));

                if (selectedForumIds != null)
                {
                    query = query.Where(p => selectedForumIds.Contains(p.Thread.ForumId));
                }

                query = query.OrderByDescending(p => p.Created);

                return query.Select(p => p.Id);
            }

            public IQueryable<PostUpdateModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<Post>()
                        .Where(p => ids.Contains(p.Id))
                        .Select(p => new PostUpdateModel
                        {
                            PostId = p.Id,
                            UserId = p.UserId,
                            Author = p.User.Name,
                            Text = p.Text,
                            ThreadId = p.ThreadId,
                            ThreadName = p.Thread.Name,
                            Created = p.Created,
                            ForumId = p.Thread.ForumId,
                            ThreadPath = p.Thread.Path
                        });
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                if (!objectId.HasValue)
                {
                    throw new ArgumentNullException("objectId");
                }

                _nodeId = objectId.Value;
            }

            public int CollectionId { get { return DataHelper.GetCollectionIdVerified<Post>(); } }
            public bool Filtered { get { return false; } }
        }

        public long PostId { get; set; }

        [Column(ForumTextIds.Thread, CellTemplate = "~/Views/PostUpdateThreadCell.html")]
        public string ThreadName { get; set; }

        public long ThreadId { get; set; }

        public long ForumId { get; set; }

        public string Author { get; set; }

        public long? UserId { get; set; }

        public DateTime Created { get; set; }

        [Column(ForumTextIds.PostText, AllowUnsafe = true, Width = "50%")]
        public string Text { get; set; }

        public string ThreadUrl { get; set; }

        public string AuthorUrl { get; set; }

        public string ThreadPath { get; set; }

        public void PostProcess(IDomainModelProvider domainModelProvider, IDataCache dataCache)
        {
            using (var domainModel = domainModelProvider.Create())
            {
                var posts = domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == ThreadId);
                var value = dataCache.Get<IUserCache>().Nodes.FirstOrDefault(n => n.Item1.NodeId == ForumId);
                if (value == null)
                {
                    return;
                }
                if (value.Item2 != NodeAccessType.Manage)
                {
                    posts = posts.Where(p => !p.Removed);
                }

                var postIds = posts.OrderBy(p => p.Created).Select(p => p.Id).ToList();

                var url = string.Format("{0}/{1}", value.Item1.FullPath, ThreadPath);

                var recordsPerPage = dataCache.Get<ISiteConfiguration>().RecordsPerPage;

                var postIndex = postIds.IndexOf(PostId);

                if (postIndex < 0)
                {
                    postIndex = 0;
                }

                var page = (postIndex / recordsPerPage) + 1;

                ThreadUrl = RecordListHelper.PageLink(url, page);

                if (UserId.HasValue)
                {
                    AuthorUrl = UserHelper.GetUserProfileLink(UserId.Value);
                }
            }
        }
    }
}
