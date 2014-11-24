/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.DataObjects.Structure
{
    public class Node : DataObject
    {
        public string Path { get; set; }
        [TextSearch]
        public string Title { get; set; }
        [TextSearch]
        public string Description { get; set; }
        public string HandlerId { get; set; }
        public string Settings { get; set; }
        public bool Enabled { get; set; }
        public bool AddToMenu { get; set; }
        public int Order { get; set; }
        public long? ParentId { get; set; }
    }
}
