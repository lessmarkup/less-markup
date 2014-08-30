/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

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

        [TextSearch]
        public string Text { get; set; }

        public List<PostAttachment> Attachments { get; set; }
        public List<PostHistory> Histories { get; set; } 
    }
}
