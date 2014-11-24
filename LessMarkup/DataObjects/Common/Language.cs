﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class Language : DataObject
    {
        public string Name { get; set; }
        public long? IconId { get; set; }
        public string ShortName { get; set; }
        public bool Visible { get; set; }
        public bool IsDefault { get; set; }
    }
}
