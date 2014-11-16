/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.Model;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class PostUpdatesNodeHandler : RecordListWithNotifyNodeHandler<PostUpdateModel>
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private readonly IDomainModelProvider _domainModelProvider;

        public PostUpdatesNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
            : base(domainModelProvider, dataCache, currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
            _domainModelProvider = domainModelProvider;

            AddCreateAction<ForumSubscriptionModel>("EditSubscription", Constants.ModuleType.Forum, ForumTextIds.Subscribe);
        }

        public override string Title
        {
            get { return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.PostUpdatesTitle); }
        }

        public override string Tooltip
        {
            get { return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.PostUpdatesTooltip); }
        }

        public override string Icon
        {
            get { return "glyphicon-comment"; }
        }

        public object EditSubscription(ForumSubscriptionModel newObject)
        {
            if (!ObjectId.HasValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (newObject == null)
            {
                var model = DependencyResolver.Resolve<ForumSubscriptionModel>();
                model.Load(ObjectId.Value);
                return ReturnNewObjectResult(model);
            }

            newObject.Save(ObjectId.Value);
            return null;
        }

        public override int GetValueChange(long? fromVersion, long? toVersion, IDomainModel domainModel)
        {
            if (!fromVersion.HasValue)
            {
                return 0;
            }

            var userId = _currentUser.UserId;

            if (!userId.HasValue)
            {
                return 0;
            }

            var changesCache = _dataCache.Get<IChangesCache>();
            var collection = GetCollection();

            var changes = changesCache.GetCollectionChanges(collection.CollectionId, fromVersion, toVersion, change =>
            {
                if (userId.HasValue && change.UserId == userId)
                {
                    return false;
                }
                return change.Type != EntityChangeType.Removed;
            });

            if (changes == null)
            {
                return 0;
            }

            var changeIds = changes.Select(c => c.EntityId).Distinct().ToList();

            return collection.ReadIds(domainModel, null, true).Count(r => changeIds.Contains(r));
        }

        protected override void PostProcessRecords(List<PostUpdateModel> records)
        {
            foreach (var record in records)
            {
                record.PostProcess(_domainModelProvider, _dataCache);
            }
        }
    }
}
