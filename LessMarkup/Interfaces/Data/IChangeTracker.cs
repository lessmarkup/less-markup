/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Data
{
    public delegate void RecordChangeHandler(long recordId, long? userId, long entityId, EntityType entityType, EntityChangeType entityChange, long? siteId);

    public interface IChangeTracker
    {
        void AddChange(long entityId, EntityType entityType, EntityChangeType changeType, IDomainModel domainModel);

        event RecordChangeHandler RecordChanged;
    }
}
