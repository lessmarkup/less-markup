/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Forum.DataObjects
{
    public class Post : DataObject
    {
        public bool Removed { get; set; }
        public long ThreadId { get; set; }
        public string IpAddress { get; set; }
        public DateTime Created { get; set; }
        public long? UserId { get; set; }
        [TextSearch]
        public string Text { get; set; }
    }
}
