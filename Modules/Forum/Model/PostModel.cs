/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Security;
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
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = ForumTextIds.Edit, DataType = typeof(Post))]
    public class PostModel
    {
        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly IDataCache _dataCache;
        private readonly IHtmlSanitizer _htmlSanitizer;

        public class Collection : AbstractModelCollection<PostModel>
        {
            private long _threadId;
            private NodeAccessType _accessType;

            public Collection() : base(typeof (Post))
            {
            }

            public override IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                if (_accessType == NodeAccessType.NoAccess)
                {
                    return new List<long>();
                }

                query = query.From<Post>().Where("ThreadId = $", _threadId);

                if (_accessType != NodeAccessType.Manage)
                {
                    query = query.Where("Removed = $", false);
                }

                return query.ToIdList();
            }

            public override IReadOnlyCollection<PostModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                if (_accessType == NodeAccessType.NoAccess)
                {
                    return new List<PostModel>();
                }

                query = query.From<Post>("p").LeftJoin<User>("u", "u.Id = p.UserId").Where(string.Format("p.[ThreadId] = {0} AND p.Id IN ({1})", _threadId, string.Join(",", ids)));

                if (_accessType != NodeAccessType.Manage)
                {
                    query = query.Where("Removed = $", false);
                }

                var ret = query.ToList<PostModel>("p.Text, u.Name UserName, p.Id PostId, p.Removed, p.Created, p.UserId");

                if (ret.Count > 0)
                {
                    var attachments = query.New()
                        .From<PostAttachment>()
                        .Where(string.Format("PostId IN ({0})", string.Join(",", ret.Select(p => p.PostId))))
                        .ToList<PostAttachment>()
                        .GroupBy(a => a.PostId)
                        .ToDictionary(p => p.Key,
                            p => p.Select(pa => new PostAttachmentModel {FileName = pa.FileName, Id = pa.Id}).ToList());

                    foreach (var post in ret)
                    {
                        List<PostAttachmentModel> list;
                        if (attachments.TryGetValue(post.PostId, out list))
                        {
                            post.Attachments = list;
                        }
                        else
                        {
                            post.Attachments = new List<PostAttachmentModel>();
                        }
                    }
                }

                return ret;
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

        public PostModel(ILightDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IDataCache dataCache, IHtmlSanitizer htmlSanitizer)
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _dataCache = dataCache;
            _htmlSanitizer = htmlSanitizer;
        }

        private void OnDeletePost(long threadId, ILightDomainModel domainModel)
        {
            var lastPost = domainModel.Query().From<Post>()
                .Where("ThreadId = $ AND Removed = $", threadId, false)
                .OrderByDescending("Created")
                .FirstOrDefault<Post>();

            var thread = domainModel.Query().Find<Thread>(threadId);

            if (lastPost == null)
            {
                thread.Removed = true;
            }
            else
            {
                thread.Updated = lastPost.Created;
            }

            _changeTracker.AddChange(thread, EntityChangeType.Updated, domainModel);
            domainModel.Update(thread);
        }

        private void OnAddPost(long threadId, ILightDomainModel domainModel)
        {
            var lastPost = domainModel.Query().From<Post>()
                .Where("ThreadId = $ AND Removed = $", threadId, false)
                .OrderByDescending("Created")
                .First<Post>();

            var thread = domainModel.Query().Find<Thread>(threadId);
            thread.Updated = lastPost.Created;
            _changeTracker.AddChange(thread, EntityChangeType.Updated, domainModel);
            domainModel.Update(thread);
        }

        public void DeletePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.Query().From<Post>().Where("ThreadId = $ AND Id = $ AND Removed = $", threadId, postId, false).First<Post>();
                post.Removed = true;
                UserId = post.UserId;
                domainModel.Update(post);
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Removed, domainModel);
                OnDeletePost(threadId, domainModel);
            }

            _changeTracker.Invalidate();
        }

        public void RestorePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.Query().From<Post>().Where("ThreadId = $ AND Id = $ AND Removed = $", threadId, postId, true).First<Post>();
                post.Removed = false;
                UserId = post.UserId;
                domainModel.Update(post);
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Added, domainModel);
                OnAddPost(threadId, domainModel);
            }

            _changeTracker.Invalidate();
        }

        public void PurgePost(long threadId, long postId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.Query().From<Post>().Where("ThreadId = $ AND Id = $", threadId, postId).First<Post>();
                UserId = post.UserId;
                domainModel.Delete<Post>(post.Id);
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Removed, domainModel);
                OnDeletePost(threadId, domainModel);
            }

            _changeTracker.Invalidate();
        }

        public void EditPost(long threadId, long postId)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var model = modelCache.GetDefinition<PostModel>();
            model.ValidateInput(this, false, null);

            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.Query().From<Post>().Where("ThreadId = $ AND Id = $ AND Removed = $", threadId, postId, false).First<Post>();
                post.Text = _htmlSanitizer.Sanitize(Text, new List<string> { "blockquote>header" });
                Text = post.Text;
                domainModel.Update(post);
                _changeTracker.AddChange<Post>(postId, EntityChangeType.Updated, domainModel);
                OnAddPost(threadId, domainModel);
            }
        }

        public void PostProcess(IDataCache dataCache, string fullPath)
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

            foreach (var attachment in Attachments)
            {
                attachment.Url = fullPath + "/attachments/" + PostId + "/" + attachment.Id;
            }
        }

        public long PostId { get; set; }

        [Column(ForumTextIds.Author, CellTemplate = "~/Views/PostAuthorCell.html", Scope = "users[row.userId]", Width = "15%", Align = Align.Center)]
        public string UserName { get; set; }

        public long? UserId { get; set; }

        public bool CanManage { get; set; }

        public bool CanEdit { get; set; }

        public bool Removed { get; set; }

        public DateTime Created { get; set; }

        public List<PostAttachmentModel> Attachments { get; set; }
            
        [Column(ForumTextIds.PostText, CellTemplate = "~/Views/PostTextCell.html", AllowUnsafe = true, Width = "*")]
        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        [RecordSearch]
        public string Text { get; set; }
    }
}
