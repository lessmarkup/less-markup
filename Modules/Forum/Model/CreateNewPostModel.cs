/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Forum.Model
{
    [RecordModel(TitleTextId = ForumTextIds.NewPost)]
    public class CreateNewPostModel
    {
        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        public string Text { get; set; }

        [InputField(InputFieldType.FileList, ForumTextIds.Attachments)]
        public List<InputFile> Attachments { get; set; } 

        public long? UserId { get; set; }
        public long PostId { get; set; }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private readonly IHtmlSanitizer _htmlSanitizer;
        private readonly IUserSecurity _userSecurity;
        private readonly ISiteConfiguration _siteConfiguration;

        public CreateNewPostModel(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IDataCache dataCache, ICurrentUser currentUser, IHtmlSanitizer htmlSanitizer, IUserSecurity userSecurity, ISiteConfiguration siteConfiguration)
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _dataCache = dataCache;
            _currentUser = currentUser;
            _htmlSanitizer = htmlSanitizer;
            _userSecurity = userSecurity;
            _siteConfiguration = siteConfiguration;
        }

        public void CreatePost(long threadId)
        {
            if (Attachments != null)
            {
                foreach (var attachment in Attachments)
                {
                    _userSecurity.ValidateInputFile(attachment);
                }
            }

            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition<CreateNewPostModel>();

            definition.ValidateInput(this, true, null);

            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.GetSiteCollection<Thread>().Single(t => t.Id == threadId);

                var post = new Post
                {
                    ThreadId = threadId,
                    Created = DateTime.UtcNow,
                    Text = _htmlSanitizer.Sanitize(Text, new List<string> {"blockquote>header"}),
                    UserId = _currentUser.UserId,
                    IpAddress = HttpContext.Current.Request.UserHostAddress
                };

                domainModel.AddSiteObject(post);
                domainModel.SaveChanges();
                _changeTracker.AddChange(post, EntityChangeType.Added, domainModel);

                if (Attachments != null)
                {
                    foreach (var source in Attachments)
                    {
                        if (source.Type.ToLower().StartsWith("image/"))
                        {
                            ImageUploader.ReduceToAllowedImageSize(source, _siteConfiguration);
                        }

                        var attachment = new PostAttachment
                        {
                            ContentType = source.Type,
                            FileName = source.Name,
                            Data = source.File,
                            PostId = post.Id,
                        };

                        domainModel.AddSiteObject(attachment);
                    }
                }

                thread.Updated = post.Created;

                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();

                PostId = post.Id;
                UserId = post.UserId;
            }

            _changeTracker.Invalidate();
        }
    }
}
