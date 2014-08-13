/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Structure
{
    public class Node : SiteDataObject
    {
        public string Path { get; set; }
        public string Title { get; set; }
        public string HandlerId { get; set; }
        public string Settings { get; set; }
        public bool Enabled { get; set; }
        public int Order { get; set; }
        public int Level { get; set; }
        public List<NodeAccess> NodeAccess { get; set; }
    }
}
