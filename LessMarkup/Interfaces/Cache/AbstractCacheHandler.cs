/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Cache
{
    public abstract class AbstractCacheHandler : ICacheHandler
    {
        private readonly Type[] _handledCollectionTypes;

        protected AbstractCacheHandler(Type [] handledCollectionTypes)
        {
            _handledCollectionTypes = handledCollectionTypes;
        }

        void ICacheHandler.Initialize(long? siteId, long? objectId)
        {
            Initialize(siteId, objectId);
        }

        protected abstract void Initialize(long? siteId, long? objectId);

        bool ICacheHandler.Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            return Expires(collectionId, entityId, changeType);
        }

        protected virtual bool Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            return true;
        }

        Type[] ICacheHandler.HandledCollectionTypes { get { return _handledCollectionTypes; } }

        protected bool Expired { get; set; }

        bool ICacheHandler.Expired { get { return Expired; } }
    }
}
