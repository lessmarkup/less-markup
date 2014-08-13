/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class EntityChangeHistory : NonSiteDataObject
    {
        public long? UserId { get; set; }
        public long EntityId { get; set; }
        public int CollectionId { get; set; }
        public int ChangeType { get; set; } // EntityChangeType
        public DateTime Created { get; set; }
        public long? SiteId { get; set; }
    }
}
