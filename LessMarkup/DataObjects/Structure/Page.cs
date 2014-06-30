/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.DataFramework.DataObjects;

namespace LessMarkup.DataObjects.Structure
{
    public class Page : SiteDataObject
    {
        public long PageId { get; set; }
        public string Path { get; set; }
        public string Title { get; set; }
        public string HandlerId { get; set; }
        public string Settings { get; set; }
        public bool Enabled { get; set; }
        public int Order { get; set; }
        public int Level { get; set; }
        public List<PageAccess> PageAccess { get; set; }
    }
}
