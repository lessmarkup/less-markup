/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Security;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Forum.Model;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;
using UserModel = LessMarkup.Forum.Model.UserModel;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class ThreadNodeHandler : RecordListNodeHandler<PostModel>
    {
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ICurrentUser _currentUser;

        public ThreadNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
            : base(domainModelProvider, dataCache, currentUser)
        {
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
            _currentUser = currentUser;
        }

        protected override void AddEditActions()
        {
            // We create own edit actions so skip this logic

            if (HasWriteAccess)
            {
                AddCreateAction<CreateNewPostModel>("NewPost", Constants.ModuleType.Forum, ForumTextIds.NewPost);
            }
        }

        [RecordAction(ForumTextIds.Reply, CreateType = typeof(PostReplyModel), Initialize = true, MinimumAccess = NodeAccessType.Write, Visible = "!removed")]
        public object CreateReply(long recordId, PostReplyModel newObject, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            if (newObject == null)
            {
                // pre-initialize object
                newObject = DependencyResolver.Resolve<PostReplyModel>();
                return newObject.Initialize(ObjectId.Value, recordId, HasWriteAccess);
            }

            newObject.CreateReply(ObjectId.Value, recordId, HasWriteAccess);

            var ret = ReturnRecordResult(newObject.PostId, true);

            if (newObject.UserId.HasValue)
            {
                UserModel.FillUsers(ret, _dataCache, newObject.UserId.Value);
            }

            if (!newObject.MoveToLastMessage)
            {
                ret["page"] = "current";
            }

            return ret;
        }

        [RecordAction(ForumTextIds.Edit, Visible = "!removed", MinimumAccess = NodeAccessType.Manage, CreateType = typeof(PostModel), Initialize = true)]
        public object EditPost(long recordId, PostModel newObject, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            if (newObject == null)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    newObject = GetCollection().Read(domainModel, new List<long> {recordId}).First();
                    return ReturnRecordResult(newObject);
                }
            }

            newObject.EditPost(ObjectId.Value, recordId);

            var ret = ReturnRecordResult(newObject);

            if (newObject.UserId.HasValue)
            {
                UserModel.FillUsers(ret, _dataCache, newObject.UserId.Value);
            }

            return ret;
        }

        [RecordAction(ForumTextIds.Delete, Visible = "canManage && !removed", MinimumAccess = NodeAccessType.Manage)]
        public object DeletePost(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postModel = DependencyResolver.Resolve<PostModel>();
            postModel.DeletePost(ObjectId.Value, recordId);

            var ret = ReturnRecordResult(recordId);

            if (postModel.UserId.HasValue)
            {
                UserModel.FillUsers(ret, _dataCache, postModel.UserId.Value);
            }

            return ret;
        }

        [RecordAction(ForumTextIds.Restore, Visible = "canManage && removed", MinimumAccess = NodeAccessType.Manage)]
        public object RestorePost(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postModel = DependencyResolver.Resolve<PostModel>();
            postModel.RestorePost(ObjectId.Value, recordId);

            var ret = ReturnRecordResult(recordId);

            if (postModel.UserId.HasValue)
            {
                UserModel.FillUsers(ret, _dataCache, postModel.UserId.Value);
            }

            return ret;
        }

        [RecordAction(ForumTextIds.Purge, MinimumAccess = NodeAccessType.Manage, Visible = "removed")]
        public object PurgePost(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postModel = DependencyResolver.Resolve<PostModel>();
            postModel.PurgePost(ObjectId.Value, recordId);

            var ret = ReturnRemovedResult();

            if (postModel.UserId.HasValue)
            {
                UserModel.FillUsers(ret, _dataCache, postModel.UserId.Value);
            }

            return ret;
        }

        [ActionAccess(NodeAccessType.Write)]
        public object NewPost(CreateNewPostModel newObject, string filter)
        {
            if (newObject == null)
            {
                return ReturnNewObjectResult(DependencyResolver.Resolve<CreateNewPostModel>());
            }

            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            newObject.CreatePost(ObjectId.Value);

            var ret = ReturnRecordResult(newObject.PostId, true);

            if (newObject.UserId.HasValue)
            {
                UserModel.FillUsers(ret, _dataCache, newObject.UserId.Value);
            }

            return ret;
        }

        [RecordAction(ForumTextIds.Block, CreateType = typeof(UserBlockModel), MinimumAccess = NodeAccessType.Manage, Visible = "userId != null")]
        public object BlockUser(long recordId, UserBlockModel newObject)
        {
            if (newObject == null)
            {
                return ReturnNewObjectResult(DependencyResolver.Resolve<UserBlockModel>());
            }

            newObject.InternalReason = string.Format("In response for post {0}", recordId);
            long userId;

            using (var domainModel = _domainModelProvider.Create())
            {
                var post = domainModel.GetSiteCollection<Post>().Single(p => p.Id == recordId);

                if (!post.UserId.HasValue)
                {
                    return ReturnMessageResult(LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.UserNotDefined));
                }

                userId = post.UserId.Value;
            }

            newObject.BlockUser(null, userId);

            return null;
        }

        protected override void PostProcessRecord(PostModel record)
        {
            base.PostProcessRecord(record);

            record.PostProcess(_dataCache, FullPath);

            record.CanManage = HasManageAccess;
            record.CanEdit = HasManageAccess;
        }

        protected override void ReadRecords(Dictionary<string, object> values, List<long> ids, IDomainModel domainModel)
        {
            base.ReadRecords(values, ids, domainModel);
            UserModel.FillUsersFromPosts(values, _dataCache, domainModel, ids);
        }

        protected override string ExtensionScript
        {
            get { return "extensions/postlist"; }
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            if (!ObjectId.HasValue)
            {
                return null;
            }

            var parts = path.Split(new[] {'/'});

            if (parts.Length != 3 || parts[0] != "attachments")
            {
                return null;
            }

            long postId;
            long attachmentId;

            if (!long.TryParse(parts[1], out postId) || !long.TryParse(parts[2], out attachmentId))
            {
                return null;
            }

            var handler = DependencyResolver.Resolve<PostAttachmentsNodeHandler>();

            ((INodeHandler) handler).Initialize(null, null, null, null, null, AccessType);

            handler.Initialize(ObjectId.Value, postId, attachmentId);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Path = path,
            };
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var result = base.GetViewData();

            var userId = _currentUser.UserId;

            if (userId.HasValue && ObjectId.HasValue)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var properties = domainModel.GetSiteCollection<UserPropertyDefinition>().Select(p => new UserPropertyModel
                        {
                            Name = p.Name,
                            Title = p.Title,
                            Type = p.Type.ToString()
                        }).ToList();

                    result["userProperties"] = properties;

                    foreach (var property in properties)
                    {
                        property.Name = property.Name.ToJsonCase();
                    }

                    var lastRead = domainModel.GetSiteCollection<Thread>()
                        .Where(t => t.Id == ObjectId)
                        .Select(t => t.Views.Where(v => v.UserId == userId.Value).Max(v => v.Updated))
                        .First();

                    result["lastRead"] = lastRead;

                    var view = domainModel.GetSiteCollection<ThreadView>()
                        .Where(v => v.UserId == userId.Value && v.ThreadId == ObjectId.Value)
                        .OrderByDescending(v => v.LastSeen)
                        .FirstOrDefault(v => v.UserId == userId);
                    if (view == null)
                    {
                        view = new ThreadView
                        {
                            ThreadId = ObjectId.Value, 
                            UserId = userId.Value,
                            Views = 1
                        };
                        domainModel.GetSiteCollection<ThreadView>().Add(view);
                    }
                    else
                    {
                        if (DateTime.UtcNow.AddMinutes(-ThreadModel.ActiveUserThresholdMinutes) > view.LastSeen)
                        {
                            view.Views++;
                        }
                    }

                    view.LastSeen = DateTime.UtcNow;

                    domainModel.SaveChanges();
                }
            }

            return result;
        }

        public object UpdateRead(long threadId, DateTime lastRead)
        {
            var userId = _currentUser.UserId;

            if (!userId.HasValue || !ObjectId.HasValue)
            {
                return null;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var view = domainModel.GetSiteCollection<ThreadView>().FirstOrDefault(u => u.UserId == userId.Value && u.ThreadId == ObjectId.Value);

                if (view == null)
                {
                    view = new ThreadView
                    {
                        ThreadId = ObjectId.Value,
                        UserId = userId.Value
                    };
                    domainModel.GetSiteCollection<ThreadView>().Add(view);
                }

                view.LastSeen = DateTime.UtcNow;
                view.Updated = lastRead;
                domainModel.SaveChanges();
            }

            return null;
        }
    }
}
