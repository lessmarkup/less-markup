/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Security
{
    public class FailedLoginHistory : NonSiteDataObject
    {
        public long FailedLoginHistoryId { get; set; }
        public long? UserId { get; set; }
        public string Address { get; set; }
        public int AttemptCount { get; set; }
        public DateTime LastAccess { get; set; }
    }
}
