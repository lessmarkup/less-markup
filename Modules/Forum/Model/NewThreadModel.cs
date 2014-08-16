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
    [RecordModel(TitleTextId = ForumTextIds.NewThread)]
    public class NewThreadModel
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ICurrentUser _currentUser;
        private readonly IChangeTracker _changeTracker;

        public NewThreadModel(IDomainModelProvider domainModelProvider, ICurrentUser currentUser, IChangeTracker changeTracker)
        {
            _domainModelProvider = domainModelProvider;
            _currentUser = currentUser;
            _changeTracker = changeTracker;
        }

        [InputField(InputFieldType.Text, ForumTextIds.ThreadTitle, Required = true)]
        public string Title { get; set; }

        [InputField(InputFieldType.Text, ForumTextIds.Description)]
        public string Description { get; set; }

        [InputField(InputFieldType.RichText, ForumTextIds.ThreadPost, Required = true)]
        public string Post { get; set; }

        public string CreateThread(long forumId)
        {
            string ret;

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var thread = new Thread
                {
                    Created = DateTime.UtcNow,
                    Description = Description,
                    Name = Title,
                };

                thread.Updated = thread.Created;
                thread.ForumId = forumId;

                var basePathName = TextToUrl.Generate(thread.Name);

                var siblingNames = domainModel.GetSiteCollection<Thread>().Where(t => t.ForumId == forumId).Select(t => t.Path).ToList();

                for (int i = 1; ; i++)
                {
                    thread.Path = i == 1 ? basePathName : string.Format("{0}-{1}", basePathName, i);
                    if (!siblingNames.Contains(thread.Path))
                    {
                        break;
                    }
                }

                thread.Posts = new List<Post>();

                var post = domainModel.GetSiteCollection<Post>().Create();

                post.Created = thread.Created;
                post.Text = Post;
                post.Removed = false;
                post.UserId = _currentUser.UserId;
                post.Updated = thread.Created;

                thread.Posts.Add(post);

                domainModel.GetSiteCollection<Thread>().Add(thread);

                domainModel.SaveChanges();

                _changeTracker.AddChange(thread, EntityChangeType.Added, domainModel);
                _changeTracker.AddChange(post, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                domainModel.CompleteTransaction();

                ret = thread.Path;
            }

            _changeTracker.Invalidate();

            return ret;
        }
    }
}
