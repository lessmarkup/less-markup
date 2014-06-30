/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Cache
{
    /// <summary>
    /// Interface for specific cache handler
    /// </summary>
    public interface ICacheHandler
    {
        /// <summary>
        /// Initializes the cache time.
        /// </summary>
        /// <param name="expirationTime">The expiration time.</param>
        /// <param name="objectId">The object identifier.</param>
        void Initialize(out DateTime? expirationTime, long? objectId = null);
        /// <summary>
        /// Expires the cache item.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="changeType">Type of the change.</param>
        /// <returns></returns>
        bool Expires(EntityType entityType, long entityId, EntityChangeType changeType);
        /// <summary>
        /// Gets the list of types handled by this cache handler.
        /// </summary>
        /// <value>
        /// The handled types.
        /// </value>
        EntityType[] HandledTypes { get; }
    }
}
