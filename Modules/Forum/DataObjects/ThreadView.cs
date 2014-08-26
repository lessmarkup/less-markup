/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.DataObjects
{
    public class ThreadView : SiteDataObject
    {
        [ForeignKey("Thread")]
        public long ThreadId { get; set; }
        public Thread Thread { get; set; }

        public int Views { get; set; }

        [ForeignKey("User")]
        public long? UserId { get; set; }
        public User User { get; set; }

        public DateTime Updated { get; set; }
    }
}
