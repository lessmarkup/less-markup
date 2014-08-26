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
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class ForumNodeHandler : NewRecordListNodeHandler<ThreadModel>
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;

        public ForumNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
            : base(domainModelProvider, dataCache, currentUser)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
        }

        protected override string ViewType
        {
            get { return "Forum"; }
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

        protected override Dictionary<string, object> GetViewData()
        {
            var ret = base.GetViewData();

            var statistics = DependencyResolver.Resolve<AllForumsStatistics>();

            if (ObjectId.HasValue)
            {
                statistics.CollectStatistics(ObjectId.Value, typeof(ForumNodeHandler));
                statistics.OrganizeGroups(false);
            }

            ret["Groups"] = statistics.Groups;
            ret["IsSubForum"] = true;

            var settings = GetSettings<ForumConfigurationModel>();

            ret["HasThreads"] = settings == null || settings.HasThreads;

            return ret;
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

            handler.Initialize(thread.ThreadId, null, null, path, FullPath + "/" + path, AccessType);

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

            var threadPath = newObject.CreateThread(ObjectId.Value);

            var nodeCache = _dataCache.Get<INodeCache>();
            var node = nodeCache.GetNode(ObjectId.Value);

            var url = string.Format("{0}/{1}", node.FullPath, threadPath);

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

        protected override void PostProcessRecord(ThreadModel record)
        {
            record.PostProcess();
        }

        protected override Type SettingsModel
        {
            get { return typeof(ForumConfigurationModel); }
        }
    }
}
