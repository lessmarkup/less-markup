/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class Currency : SiteDataObject
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public double Rate { get; set; }

        public bool Enabled { get; set; }

        public bool IsBase { get; set; }

        public DateTime LastUpdate { get; set; }
    }
}
