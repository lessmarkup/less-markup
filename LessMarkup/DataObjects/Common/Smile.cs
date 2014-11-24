/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class Smile : DataObject
    {
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public string Name { get; set; }
        [MaxLength(50)]
        public string Code { get; set; }
    }
}
