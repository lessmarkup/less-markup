/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Security;
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

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                var userId = _currentUser.UserId;

                List<long> selectedForumIds = null;

                if (userId.HasValue)
                {
                    var nodeUserData = query.New().From<NodeUserData>().Where("NodeId = $ AND UserId = $", _nodeId, userId.Value).FirstOrDefault<NodeUserData>();
                        
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
                        return new List<long>();
                    }
                }

                var nodeCache = _dataCache.Get<INodeCache>();
                var collectionId = DataHelper.GetCollectionId<Post>();

                var changesCache = _dataCache.Get<IChangesCache>();
                var changesQuery = userId.HasValue ? 
                    changesCache.GetCollectionChanges(collectionId, null, null, f => f.Type != EntityChangeType.Removed && f.UserId != userId) : 
                    changesCache.GetCollectionChanges(collectionId, null, null, f => f.Type != EntityChangeType.Removed);

                if (changesQuery == null)
                {
                    return new List<long>();
                }

                var changesIds = changesQuery.Select(c => c.EntityId).ToList();
                var nodeIds = nodeCache.Nodes.Where(n => n.CheckRights(_currentUser) != NodeAccessType.NoAccess ).Select(n => n.NodeId).ToList();

                query = query.From<Post>("p").Join<Thread>("t", "p.ThreadId = t.Id").Where("p.Removed = $ AND t.ForumId IN ($) AND p.Id IN ($)", false, string.Join(",", nodeIds),
                            string.Join(",", changesIds));

                if (selectedForumIds != null)
                {
                    query = query.Where("t.ForumId IN ($)", string.Join(",", selectedForumIds));
                }

                query = query.OrderByDescending("Created");

                return query.ToList<Post>("p.Id").Select(p => p.Id).ToList();
            }

            public IReadOnlyCollection<PostUpdateModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<Post>("p")
                    .Join<Thread>("t", "t.Id = p.ThreadId")
                    .Join<User>("u", "u.Id = p.UserId")
                    .Where("p.Id IN ($)", string.Join(",", ids))
                    .ToList<PostUpdateModel>(
                        "p.Id PostId, p.UserId, u.Name Author, p.Text, p.ThreadId, t.Name ThreadName, p.Created, t.ForumId, t.Path ThreadPath");
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                if (!objectId.HasValue)
                {
                    throw new ArgumentNullException("objectId");
                }

                _nodeId = objectId.Value;
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<Post>(); } }
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

        public void PostProcess(ILightDomainModelProvider domainModelProvider, IDataCache dataCache)
        {
            using (var domainModel = domainModelProvider.Create())
            {
                var value = dataCache.Get<IUserCache>().Nodes.FirstOrDefault(n => n.Item1.NodeId == ForumId);
                if (value == null)
                {
                    return;
                }

                var query = domainModel.Query().From<Post>().Where("ThreadId = $", ThreadId);

                if (value.Item2 != NodeAccessType.Manage)
                {
                    query = query.Where("Removed = $", false);
                }
                
                var posts = query.ToList<Post>();

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
