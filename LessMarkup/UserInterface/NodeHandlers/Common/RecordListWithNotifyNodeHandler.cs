/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class RecordListWithNotifyNodeHandler<T> : RecordListNodeHandler<T>, INotificationProvider where T : class
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        protected RecordListWithNotifyNodeHandler(ILightDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser) : base(domainModelProvider, dataCache)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public abstract string Title { get; }
        public abstract string Tooltip { get; }
        public abstract string Icon { get; }

        public virtual int GetValueChange(long? fromVersion, long? toVersion, ILightDomainModel domainModel)
        {
            var changesCache = _dataCache.Get<IChangesCache>();
            var userId = _currentUser.UserId;

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

            return collection.ReadIds(domainModel.Query().WhereIds(changeIds), true).Count();
        }

        protected override bool SupportsLiveUpdates
        {
            get { return false; }
        }

        protected override bool SupportsManualRefresh
        {
            get { return true; }
        }
    }
}
