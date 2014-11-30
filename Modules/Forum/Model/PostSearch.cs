/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Forum.Model
{
    public class PostSearch : IEntitySearch
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public PostSearch(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        class PostAndThread
        {
            public long ForumId { get; set; }
            public string Path { get; set; }
            public long ThreadId { get; set; }
        }

        public string ValidateAndGetUrl(int collectionId, long entityId, IDomainModel domainModel)
        {
            var post = domainModel.Query().From<Post>("p").Join<Thread>("t", "p.ThreadId = t.Id").Where("p.Id = $", entityId).FirstOrDefault<PostAndThread>("t.ForumId, t.Path, p.ThreadId");
            if (post == null)
            {
                return null;
            }

            var nodeCache = _dataCache.Get<INodeCache>();

            var node = nodeCache.GetNode(post.ForumId);

            if (node == null)
            {
                return null;

            }
            
            var rights = node.CheckRights(_currentUser);

            if (rights == NodeAccessType.NoAccess)
            {
                return null;
            }

            var url = string.Format("{0}/{1}", node.FullPath, post.Path);

            var postsQuery = domainModel.Query().From<Post>().Where("ThreadId = $", post.ThreadId);
            
            if (rights != NodeAccessType.Manage)
            {
                postsQuery = postsQuery.Where("Removed = $", false);
            }
            
            var posts = postsQuery.ToIdList().ToList();

            var recordsPerPage = _dataCache.Get<ISiteConfiguration>().RecordsPerPage;

            var postIndex = posts.IndexOf(entityId);

            if (postIndex < 0)
            {
                postIndex = 0;
            }

            var page = (postIndex / recordsPerPage) + 1;

            return RecordListHelper.PageLink(url, page);
        }

        public string GetFriendlyName(int collectionId)
        {
            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.PostName);
        }
    }
}
