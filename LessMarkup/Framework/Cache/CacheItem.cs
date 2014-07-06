/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Engine.Cache
{
    internal class CacheItem
    {
        private readonly Type _type;
        private readonly DateTime? _expirationTime;
        private readonly long? _objectId;
        private readonly ICacheHandler _cachedObject;

        public CacheItem(Type type, DateTime? expirationTime, long? objectId, ICacheHandler cachedObject)
        {
            _type = type;
            _expirationTime = expirationTime;
            _objectId = objectId;
            _cachedObject = cachedObject;
        }

        public Type Type { get { return _type; } }

        public DateTime? ExpirationTime { get { return _expirationTime; } }

        public long? ObjectId { get { return _objectId; } }

        public ICacheHandler CachedObject { get { return _cachedObject; } }
    }
}
