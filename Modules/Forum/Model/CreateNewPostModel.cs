/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.Forum.Model
{
    [RecordModel(TitleTextId = ForumTextIds.NewPost)]
    public class CreateNewPostModel
    {
        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        public string Text { get; set; }

        public long? UserId { get; set; }
        public long PostId { get; set; }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private readonly IHtmlSanitizer _htmlSanitizer;

        public CreateNewPostModel(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IDataCache dataCache, ICurrentUser currentUser, IHtmlSanitizer htmlSanitizer)
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _dataCache = dataCache;
            _currentUser = currentUser;
            _htmlSanitizer = htmlSanitizer;
        }

        public void CreatePost(long threadId)
        {
            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition<CreateNewPostModel>();

            definition.ValidateInput(this, true, null);

            using (var domainModel = _domainModelProvider.Create())
            {
                var post = new Post
                {
                    ThreadId = threadId,
                    Created = DateTime.UtcNow,
                    Text = _htmlSanitizer.Sanitize(Text, new List<string> {"blockquote>header"})
                };

                post.Updated = post.Created;
                post.UserId = _currentUser.UserId;

                domainModel.GetSiteCollection<Post>().Add(post);
                domainModel.SaveChanges();
                _changeTracker.AddChange(post, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                PostId = post.Id;
                UserId = post.UserId;
            }

            _changeTracker.Invalidate();
        }
    }
}
