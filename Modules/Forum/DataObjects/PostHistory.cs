/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.DataObjects
{
    public class PostHistory : DataObject
    {
        public long PostId { get; set; }
        public string Reason { get; set; }
        public DateTime Created { get; set; }
        public string Text { get; set; }
        public long UserId { get; set; }
    }
}
