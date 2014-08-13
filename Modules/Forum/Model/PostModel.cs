/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.RecordModel;
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

        public class Collection : AbstractModelCollection<PostModel>
        {
            private long _threadId;
            private NodeAccessType _accessType;

            public Collection() : base(typeof(Post))
            { }

            public override IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                if (_accessType == NodeAccessType.Manage)
                {
                    return domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == _threadId).Select(p => p.Id);
                }

                if (_accessType == NodeAccessType.NoAccess)
                {
                    return new List<long>().AsQueryable();
                }

                return domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == _threadId && !p.Removed).Select(p => p.Id);
            }

            public override IQueryable<PostModel> Read(IDomainModel domainModel, List<long> ids)
            {
                if (_accessType == NodeAccessType.NoAccess)
                {
                    return new List<PostModel>().AsQueryable();
                }

                IQueryable<Post> collection = domainModel.GetSiteCollection<Post>().Where(p => p.ThreadId == _threadId && ids.Contains(p.Id));

                if (_accessType != NodeAccessType.Manage)
                {
                    collection = collection.Where(p => !p.Removed);
                }

                return collection.Select(p => new PostModel
                {
                    Subject = p.Subject,
                    Text = p.Text,
                    UserName = p.User.Name,
                    PostId = p.Id,
                    Removed = p.Removed,
                    Created = p.Created,
                    UserId = p.UserId
                });
            }

            public override void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                if (!objectId.HasValue)
                {
                    return;
                }

                _threadId = objectId.Value;
                _accessType = nodeAccessType;
            }

            public override bool Filtered { get { return false; } }
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
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.Id == postId && !p.Removed);

                post.Removed = true;
                UserId = post.UserId;
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Removed, domainModel);

                domainModel.SaveChanges();
            }

            _changeTracker.Invalidate();
        }

        public void RestorePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.Id == postId && p.Removed);

                post.Removed = false;
                UserId = post.UserId;
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Added, domainModel);

                domainModel.SaveChanges();
            }

            _changeTracker.Invalidate();
        }

        public void PurgePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.Id == postId);
                UserId = post.UserId;
                domainModel.GetSiteCollection<Post>().Remove(post);
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Removed, domainModel);
                domainModel.SaveChanges();
            }

            _changeTracker.Invalidate();
        }

        public void EditPost(long threadId, long postId)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var model = modelCache.GetDefinition<PostModel>();
            model.ValidateInput(this, false);

            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.Id == postId && !p.Removed);
                post.Text = _htmlSanitizer.Sanitize(Text, new List<string> { "blockquote>header" });
                post.Subject = Subject;
                Text = post.Text;
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }

        public void PostProcess(IDataCache dataCache)
        {
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

                string userName = currentUser.IsRemoved ? LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.UserRemoved) : currentUser.Name;

                userName = string.Format("<header><strong>{0} {1}</strong></header>", LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.MessageFrom), userName);

                Text = Text.Insert(end + 1, userName);

                pos = end + 1 + userName.Length;
            }
        }

        public long PostId { get; set; }

        [InputField(InputFieldType.Text, ForumTextIds.Subject)]
        public string Subject { get; set; }

        [Column(ForumTextIds.Author, CellTemplate = "~/Views/PostAuthorCell.html", Scope = "users[row.UserId]")]
        public string UserName { get; set; }

        public long? UserId { get; set; }

        public bool CanManage { get; set; }

        public bool CanEdit { get; set; }

        public bool Removed { get; set; }

        public DateTime Created { get; set; }

        [Column(ForumTextIds.PostText, CellTemplate = "~/Views/PostTextCell.html", AllowUnsafe = true, Width = "80%")]
        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        public string Text { get; set; }
    }
}
