/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class SiteCustomization : DataObject
    {
        public string Path { get; set; }
        public bool IsBinary { get; set; }
        public byte[] Body { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public bool Append { get; set; }
    }
}
