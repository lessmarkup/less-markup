/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Forum.DataObjects
{
    public class Thread : DataObject
    {
        [TextSearch]
        public string Name { get; set; }
        public string Path { get; set; }
        public long ForumId { get; set; }
        [TextSearch]
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public bool Removed { get; set; }
        public bool Closed { get; set; }
        public long? AuthorId { get; set; }
    }
}
