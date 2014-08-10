/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using LessMarkup.Forum.Model;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class ForumNodeHandler : NewRecordListNodeHandler<ThreadModel>
    {
        private readonly IDomainModelProvider _domainModelProvider;

        public ForumNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
            _domainModelProvider = domainModelProvider;
        }

        protected override void AddEditActions()
        {
        }

        protected override object Initialize(object controller)
        {
            var ret = base.Initialize(controller);

            if (HasWriteAccess)
            {
                AddCreateAction<NewThreadModel>("NewThread", Constants.ModuleType.Forum, ForumTextIds.NewThread);
            }

            return ret;
        }

        protected override bool HasChildren
        {
            get { return true; }
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            var parts = path.Split(new[] {'/'}).ToList();
            if (parts.Count == 0 || !ObjectId.HasValue)
            {
                return null;
            }

            path = parts[0];
            parts.RemoveAt(0);

            var thread = ThreadModel.GetByPath(ObjectId.Value, path, _domainModelProvider);

            if (thread == null)
            {
                return null;
            }

            var handler = (INodeHandler) DependencyResolver.Resolve<ThreadNodeHandler>();

            handler.Initialize(thread.ThreadId, null, null, path, AccessType);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Path = path,
                Id = thread.ThreadId,
                Rest = string.Join("/", parts),
                Title = thread.Name
            };
        }

        public object NewThread(NewThreadModel newObject)
        {
            if (!ObjectId.HasValue)
            {
                throw new ArgumentException("ObjectId");
            }

            if (!HasWriteAccess)
            {
                throw new UnauthorizedAccessException();
            }

            var threadId = newObject.CreateThread(ObjectId.Value, Path);

            var url = string.Format("{0}/{1}", Path, threadId);

            return ReturnRedirectResult(url);
        }

        [RecordAction(ForumTextIds.Close, MinimumAccess = NodeAccessType.Manage, Visible = "!Closed")]
        public object CloseThread(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }
            var thread = DependencyResolver.Resolve<ThreadModel>();
            return thread.Close(AccessType, ObjectId.Value, recordId, HasManageAccess);
        }

        [RecordAction(ForumTextIds.Open, MinimumAccess = NodeAccessType.Manage, Visible = "Closed")]
        public object OpenThread(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }
            var thread = DependencyResolver.Resolve<ThreadModel>();
            return thread.Open(AccessType, ObjectId.Value, recordId, HasManageAccess);
        }

        [RecordAction(ForumTextIds.Delete, MinimumAccess = NodeAccessType.Manage, Visible = "!Removed")]
        public object DeleteThread(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }
            var thread = DependencyResolver.Resolve<ThreadModel>();
            return thread.Delete(AccessType, ObjectId.Value, recordId, HasManageAccess);
        }

        [RecordAction(ForumTextIds.Restore, MinimumAccess = NodeAccessType.Manage, Visible = "Removed")]
        public object RestoreThread(long recordId, string filter)
        {
            if (!ObjectId.HasValue)
            {
                throw new NullReferenceException("ObjectId");
            }
            var thread = DependencyResolver.Resolve<ThreadModel>();
            return thread.Restore(AccessType, ObjectId.Value, recordId, HasManageAccess);
        }
    }
}
