/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class Smile : SiteDataObject
    {
        public long SmileId { get; set; }
        public ImageType ImageType { get; set; }
        public byte[] Data { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public LessMarkup.DataObjects.User.User User { get; set; }
        [ForeignKey("User")]
        public long UserId { get; set; }
        public string Name { get; set; }
        [MaxLength(50)]
        public string Code { get; set; }
        public int Order { get; set; }
    }
}
