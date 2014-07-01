/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Module;

namespace LessMarkup.Framework.FileSystem
{
    class ResourceReference
    {
        public long RecordId { get; set; }
        public bool DataLoaded { get; set; }
        public byte[] Binary { get; set; }
    }
}
