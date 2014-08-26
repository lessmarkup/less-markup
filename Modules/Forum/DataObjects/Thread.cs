/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.DataObjects
{
    public class Thread : SiteDataObject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long ForumId { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public bool Removed { get; set; }
        public bool Closed { get; set; }
        [ForeignKey("Author")]
        public long? AuthorId { get; set; }
        public User Author { get; set; }

        public List<Post> Posts { get; set; }
        public List<ThreadView> Views { get; set; } 
    }
}
