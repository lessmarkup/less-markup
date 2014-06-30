﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Statistics
{
    public class AddressCountry : NonSiteDataObject
    {
        public long AddressCountryId { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
    }
}
