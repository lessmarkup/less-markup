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

namespace LessMarkup.Forum.Model
{
    [RecordModel(TitleTextId = ForumTextIds.Reply)]
    public class PostReplyModel
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IHtmlSanitizer _htmlSanitizer;
        private readonly ICurrentUser _currentUser;
        private readonly IChangeTracker _changeTracker;

        public PostReplyModel(IDomainModelProvider domainModelProvider, IHtmlSanitizer htmlSanitizer, ICurrentUser currentUser, IChangeTracker changeTracker)
        {
            _domainModelProvider = domainModelProvider;
            _htmlSanitizer = htmlSanitizer;
            _currentUser = currentUser;
            _changeTracker = changeTracker;
        }

        public object Initialize(long threadId, long postId, bool canWrite)
        {
            if (!canWrite)
            {
                return new
                {
                    message = LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.CannotCreatePost)
                };
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.PostId == postId && !p.Removed);

                ReplyTo = post.Text;
                Subject = post.Subject;
            }

            return new
            {
                record = this
            };
        }

        public void CreateReply(long threadId, long postId, bool canWrite)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                if (!canWrite)
                {
                    throw new Exception(LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.CannotCreatePost));
                }

                var sourcePost = domainModel.GetSiteCollection<Post>().First(p => p.ThreadId == threadId && p.PostId == postId && !p.Removed);

                var newPost = new Post
                {
                    Text = string.Format("<blockquote data-from=\"{0}\">{1}</blockquote>\r\n{2}", sourcePost.UserId, sourcePost.Text,  _htmlSanitizer.Sanitize(Text, new List<string> { "blockquote>header" })),
                    Created = DateTime.UtcNow,
                    Subject = Subject,
                    ThreadId = threadId,
                    UserId = _currentUser.UserId
                };

                newPost.Updated = newPost.Created;

                domainModel.GetSiteCollection<Post>().Add(newPost);
                domainModel.SaveChanges();
                _changeTracker.AddChange(newPost.PostId, EntityType.ForumPost, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();
                PostId = newPost.PostId;
                UserId = newPost.UserId;
            }

            _changeTracker.Invalidate();
        }

        [InputField(InputFieldType.RichText, ForumTextIds.ReplyTo, ReadOnly = true)]
        public string ReplyTo { get; set; }

        [InputField(InputFieldType.Text, ForumTextIds.Subject)]
        public string Subject { get; set; }

        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        public string Text { get; set; }

        public long PostId { get; set; }

        public long? UserId { get; set; }
    }
}
