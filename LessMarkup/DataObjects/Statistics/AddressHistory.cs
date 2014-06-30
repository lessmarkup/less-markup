/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataFramework.DataObjects;

namespace LessMarkup.DataObjects.Statistics
{
    /// <summary>
    /// Represents a history per a day per an IP address
    /// </summary>
    public class AddressHistory : SiteDataObject
    {
        public long AddressHistoryId { get; set; }
        public long Date { get; set; }
        public long Ip { get; set; }
        public int Requests { get; set; }
        public int HasError { get; set; }
        public long Received { get; set; }
        public long Sent { get; set; }
        public long Created { get; set; }
        [ForeignKey("Country")]
        public long? CountryId { get; set; }
        public AddressCountry Country { get; set; }
        public string Query { get; set; }
        public string Error { get; set; }
        public int MobileRequests { get; set; }
        public int Crawler { get; set; }
        public int Resource { get; set; }
        public string UserAgent { get; set; }
    }
}
