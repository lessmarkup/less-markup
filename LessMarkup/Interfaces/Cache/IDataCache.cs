/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.Cache
{
    public interface IDataCache
    {
        T Get<T>(long? objectId = null, bool create = true) where T : ICacheHandler;
        void Expired<T>(long? objectId = null) where T : ICacheHandler;
        T CreateWithUniqueId<T>() where T : ICacheHandler;
        void Reset();
    }
}
