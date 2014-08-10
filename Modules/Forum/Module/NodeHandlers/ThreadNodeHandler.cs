/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.Model;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class ThreadNodeHandler : NewRecordListNodeHandler<PostModel>
    {
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;

        public ThreadNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
        }

        protected override void AddEditActions()
        {
            // We create own edit actions so skip this logic

            if (HasWriteAccess)
            {
                AddCreateAction<NewPostModel>("NewPost", Constants.ModuleType.Forum, ForumTextIds.NewPost);
            }
        }

        [RecordAction(ForumTextIds.Reply, CreateType = typeof(PostReplyModel), Initialize = true, MinimumAccess = NodeAccessType.Write, Visible = "!Removed")]
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

            return ReturnRecordResult(newObject.PostId, true);
        }

        [RecordAction(ForumTextIds.Edit, Visible = "!Removed", MinimumAccess = NodeAccessType.Manage, CreateType = typeof(PostModel), Initialize = true)]
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

            return ReturnRecordResult(newObject);
        }

        [RecordAction(ForumTextIds.Delete, Visible = "CanManage && !Removed", MinimumAccess = NodeAccessType.Manage)]
        public object DeletePost(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postModel = DependencyResolver.Resolve<PostModel>();
            postModel.DeletePost(ObjectId.Value, recordId);

            return ReturnRecordResult(recordId);
        }

        [RecordAction(ForumTextIds.Restore, Visible = "CanManage && Removed", MinimumAccess = NodeAccessType.Manage)]
        public object RestorePost(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postModel = DependencyResolver.Resolve<PostModel>();
            postModel.RestorePost(ObjectId.Value, recordId);

            return ReturnRecordResult(recordId);
        }

        [RecordAction(ForumTextIds.Purge, MinimumAccess = NodeAccessType.Manage)]
        public object PurgePost(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postModel = DependencyResolver.Resolve<PostModel>();
            postModel.PurgePost(ObjectId.Value, recordId);

            return ReturnRemovedResult();
        }

        [ActionAccess(NodeAccessType.Write)]
        public object NewPost(NewPostModel newObject, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }

            var postId = newObject.CreatePost(ObjectId.Value);

            return ReturnRecordResult(postId, true);
        }

        protected override void PostProcessRecord(PostModel record)
        {
            base.PostProcessRecord(record);

            record.PostProcess(_dataCache);

            record.CanManage = HasManageAccess;
            record.CanEdit = HasManageAccess;
        }
    }
}
