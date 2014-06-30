/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.DataFramework.DataObjects;

namespace LessMarkup.DataObjects.User
{
    public class UserGroup : SiteDataObject
    {
        public long UserGroupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<UserGroupMembership> Memberships { get; set; }
    }
}
