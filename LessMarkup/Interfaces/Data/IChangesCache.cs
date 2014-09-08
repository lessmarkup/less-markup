/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Data
{
    public interface IChangesCache : ICacheHandler
    {
        IEnumerable<IDataChange> GetCollectionChanges(int collectionId, long? fromId, long? toId, Func<IDataChange, bool> filterFunc = null);
        long? LastChangeId { get; }
    }
}
