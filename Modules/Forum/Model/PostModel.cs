/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Forum.Model
{
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = ForumTextIds.Edit)]
    public class PostModel
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly IDataCache _dataCache;
        private readonly IHtmlSanitizer _htmlSanitizer;

        public class Collection : IModelCollection<PostModel>
        {
            private long _threadId;
            private NodeAccessType _accessType;

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                if (_accessType == NodeAccessType.Manage)
                {
                    return domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == _threadId).Select(p => p.PostId);
                }

                if (_accessType == NodeAccessType.NoAccess)
                {
                    return new List<long>().AsQueryable();
                }

                return domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == _threadId && !p.Removed).Select(p => p.PostId);
            }

            public IQueryable<PostModel> Read(IDomainModel domainModel, List<long> ids)
            {
                if (_accessType == NodeAccessType.NoAccess)
                {
                    return new List<PostModel>().AsQueryable();
                }

                IQueryable<Post> collection = domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == _threadId && ids.Contains(p.PostId));

                if (_accessType != NodeAccessType.Manage)
                {
                    collection = collection.Where(p => !p.Removed);
                }

                return collection.Select(p => new PostModel
                {
                    Subject = p.Subject,
                    Text = p.Text,
                    AuthorId = p.UserId,
                    AuthorName = p.User.Name,
                    AuthorImageId = p.User.AvatarImageId,
                    PostId = p.PostId,
                    Removed = p.Removed,
                    Created = p.Created
                });
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                if (!objectId.HasValue)
                {
                    return;
                }

                _threadId = objectId.Value;
                _accessType = nodeAccessType;
            }

            public bool Filtered { get; private set; }
        }

        PostModel()
        {
        }

        public PostModel(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IDataCache dataCache, IHtmlSanitizer htmlSanitizer)
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _dataCache = dataCache;
            _htmlSanitizer = htmlSanitizer;
        }

        public void DeletePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.PostId == postId && !p.Removed);

                post.Removed = true;
                _changeTracker.AddChange(postId, EntityType.ForumPost, EntityChangeType.Removed, domainModel);

                domainModel.SaveChanges();
            }
        }

        public void RestorePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.PostId == postId && p.Removed);

                post.Removed = false;
                _changeTracker.AddChange(postId, EntityType.ForumPost, EntityChangeType.Added, domainModel);

                domainModel.SaveChanges();
            }
        }

        public void PurgePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.PostId == postId);
                domainModel.GetSiteCollection<Post>().Remove(post);
                _changeTracker.AddChange(postId, EntityType.ForumPost, EntityChangeType.Removed, domainModel);
                domainModel.SaveChanges();
            }
        }

        public void EditPost(long threadId, long postId)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var model = modelCache.GetDefinition<PostModel>();
            model.ValidateInput(this, false);

            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.PostId == postId && !p.Removed);
                post.Text = _htmlSanitizer.Sanitize(Text, new List<string> { "blockquote>header" });
                post.Subject = Subject;
                Text = post.Text;
                _changeTracker.AddChange(postId, EntityType.ForumPost, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }

        public void PostProcess(IDataCache dataCache)
        {
            if (AuthorImageId.HasValue)
            {
                AuthorImage = ImageHelper.ThumbnailUrl(AuthorImageId.Value);
            }

            if (AuthorId.HasValue)
            {
                AuthorUrl = UserHelper.GetUserProfileLink(AuthorId.Value);
                PostCount = dataCache.Get<PostStatisticsCache>().GetPostCount(AuthorId.Value);
            }

            var pos = 0;

            const string quoteSearch = "<blockquote data-from=";

            for (; ; )
            {
                pos = Text.IndexOf(quoteSearch, pos, StringComparison.Ordinal);
                if (pos < 0)
                {
                    break;
                }
                pos += quoteSearch.Length;
                var end = Text.IndexOf(">", pos, StringComparison.Ordinal);
                if (end <= 0)
                {
                    break;
                }
                long userId;
                if (!long.TryParse(Text.Substring(pos, end - pos).Trim(new[] {'"'}), out userId))
                {
                    continue;
                }

                var currentUser = dataCache.Get<IUserCache>(userId);

                string userName;

                if (currentUser.IsRemoved)
                {
                    userName = LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.UserRemoved);
                }
                else
                {
                    userName = currentUser.Name;
                }

                userName = string.Format("<header><strong>{0} {1}</strong></header>", LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.MessageFrom), userName);

                Text = Text.Insert(end + 1, userName);

                pos = end + 1 + userName.Length;
            }
        }

        public long PostId { get; set; }

        [InputField(InputFieldType.Text, ForumTextIds.Subject)]
        public string Subject { get; set; }

        [Column(ForumTextIds.Author, CellTemplate = "~/Views/PostAuthorCell.html")]
        public string AuthorName { get; set; }

        public int PostCount { get; set; }

        public long? AuthorId { get; set; }

        public string AuthorUrl { get; set; }

        public long? AuthorImageId { get; set; }

        public string AuthorImage { get; set; }

        public bool CanManage { get; set; }

        public bool CanEdit { get; set; }

        public bool Removed { get; set; }

        public DateTime Created { get; set; }

        [Column(ForumTextIds.PostText, CellTemplate = "~/Views/PostTextCell.html", AllowUnsafe = true, Width = "80%")]
        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        public string Text { get; set; }
    }
}
