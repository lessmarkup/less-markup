/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Security
{
    public class UserBlockHistory : NonSiteDataObject
    {
        public Security.User User { get; set; }
        [ForeignKey("User")]
        public long UserId { get; set; }
        public Security.User BlockedByUser { get; set; }
        [ForeignKey("BlockedByUser")]
        public long BlockedByUserId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? BlockedToTime { get; set; }
        public bool IsUnblocked { get; set; }
        public string Reason { get; set; }
    }
}
