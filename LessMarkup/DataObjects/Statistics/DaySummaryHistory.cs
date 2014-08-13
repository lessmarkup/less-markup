/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Statistics
{
    public class DaySummaryHistory : SiteDataObject
    {
        public long Day { get; set; }
        public int Requests { get; set; }
        public long Received { get; set; }
        public long Sent { get; set; }
        public int MobileRequests { get; set; }
        public int Errors { get; set; }
    }
}
