/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
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
    [RecordModel(TitleTextId = ForumTextIds.Reply)]
    public class PostReplyModel
    {
        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IHtmlSanitizer _htmlSanitizer;
        private readonly ICurrentUser _currentUser;
        private readonly IChangeTracker _changeTracker;
        private readonly IUserSecurity _userSecurity;
        private readonly ISiteConfiguration _siteConfiguration;

        public PostReplyModel(ILightDomainModelProvider domainModelProvider, IHtmlSanitizer htmlSanitizer, ICurrentUser currentUser, IChangeTracker changeTracker, IUserSecurity userSecurity, ISiteConfiguration siteConfiguration)
        {
            _domainModelProvider = domainModelProvider;
            _htmlSanitizer = htmlSanitizer;
            _currentUser = currentUser;
            _changeTracker = changeTracker;
            _userSecurity = userSecurity;
            _siteConfiguration = siteConfiguration;
            MoveToLastMessage = true;
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
                var post = domainModel.Query().From<Post>().Where("ThreadId = $ AND Id = $ AND Removed = $", threadId, postId, false).First<Post>("Text");
                ReplyTo = post.Text;
            }

            return new
            {
                record = this
            };
        }

        public void CreateReply(long threadId, long postId, bool canWrite)
        {
            if (Attachments != null)
            {
                foreach (var attachment in Attachments)
                {
                    _userSecurity.ValidateInputFile(attachment);
                }
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                if (!canWrite)
                {
                    throw new Exception(LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.CannotCreatePost));
                }

                var thread = domainModel.Query().Find<Thread>(threadId);

                var sourcePost = domainModel.Query().From<Post>().Where("ThreadId = $ AND Id = $ AND Removed = $", threadId, postId, false).First<Post>();

                var textToQuote = _htmlSanitizer.Sanitize(sourcePost.Text, null, navigable =>
                {
                    var navigator = navigable.CreateNavigator();
                    if (navigator == null)
                    {
                        return null;
                    }
                    var level = 0;
                    while (string.Compare(navigator.Name, "blockquote", StringComparison.InvariantCultureIgnoreCase) == 0 && navigator.MoveToParent())
                    {
                        level++;
                        if (level >= 2)
                        {
                            return false;
                        }
                    }
                    return null;
                });

                var newPost = new Post
                {
                    Text = string.Format("<blockquote data-from=\"{0}\">{1}</blockquote>\r\n{2}", sourcePost.UserId, textToQuote,  _htmlSanitizer.Sanitize(Text, new List<string> {"blockquote>header"})),
                    Created = DateTime.UtcNow,
                    ThreadId = threadId,
                    UserId = _currentUser.UserId,
                    IpAddress = HttpContext.Current.Request.UserHostAddress
                };

                domainModel.Create(newPost);
                _changeTracker.AddChange(newPost, EntityChangeType.Added, domainModel);

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
                            PostId = newPost.Id,
                        };

                        domainModel.Create(attachment);
                    }
                }

                thread.Updated = newPost.Created;
                domainModel.Update(thread);
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Updated, domainModel);
                PostId = newPost.Id;
                UserId = newPost.UserId;
            }

            _changeTracker.Invalidate();
        }

        [InputField(InputFieldType.RichText, ForumTextIds.ReplyTo, ReadOnly = true)]
        public string ReplyTo { get; set; }

        [InputField(InputFieldType.RichText, ForumTextIds.PostText, Required = true)]
        public string Text { get; set; }

        public long PostId { get; set; }

        public long? UserId { get; set; }

        [InputField(InputFieldType.FileList, ForumTextIds.Attachments)]
        public List<InputFile> Attachments { get; set; }

        [InputField(InputFieldType.CheckBox, ForumTextIds.ReplyMoveLastMessage, DefaultValue = true)]
        public bool MoveToLastMessage { get; set; }
    }
}
