/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Statistics
{
    public class AddressToCountry : NonSiteDataObject
    {
        public long From { get; set; }
        public long To { get; set; }
        [ForeignKey("Country")]
        public long CountryId { get; set; }
        public AddressCountry Country { get; set; }
    }
}
