/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataObjects.User;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.DataObjects
{
    public class Post : SiteDataObject
    {
        public bool Removed { get; set; }

        [ForeignKey("Thread")]
        public long ThreadId { get; set; }
        public Thread Thread { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        [ForeignKey("User")]
        public long? UserId { get; set; }
        public User User { get; set; }

        public string Subject { get; set; }

        public string Text { get; set; }
    }
}
