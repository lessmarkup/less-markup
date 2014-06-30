/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.DataFramework.DataObjects;

namespace LessMarkup.DataObjects.User
{
    public class UserPropertyDefinition : SiteDataObject
    {
        public long UserPropertyDefinitionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public UserPropertyType Type { get; set; }
        public bool IsRequired { get; set; }
        public bool VisibleInPosts { get; set; }
        public int Order { get; set; }
    }
}
