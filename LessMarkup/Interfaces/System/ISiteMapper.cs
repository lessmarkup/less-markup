/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace LessMarkup.Interfaces.System
{
    public interface ISiteMapper
    {
        long? SiteId { get; }
        string Title { get; }
        bool ModuleEnabled(string moduleType);
        void Reset();
        IEnumerable<string> ModuleTypes { get; }
    }
}
