/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataFramework.DataObjects;
using LessMarkup.DataObjects.Common;

namespace LessMarkup.DataObjects.Gallery
{
    public class GalleryImage : SiteDataObject
    {
        public long GalleryImageId { get; set; }

        [ForeignKey("Gallery")]
        public long GalleryId { get; set; }
        public Gallery Gallery { get; set; }

        [ForeignKey("Image")]
        public long ImageId { get; set; }
        public Image Image { get; set; }
    }
}
