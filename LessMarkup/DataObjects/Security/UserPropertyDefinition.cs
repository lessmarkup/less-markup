/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.DataObjects.Security
{
    public class UserPropertyDefinition : SiteDataObject
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public UserPropertyType Type { get; set; }
        public bool Required { get; set; }
    }
}
