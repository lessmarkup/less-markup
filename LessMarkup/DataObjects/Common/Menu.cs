/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class Menu : SiteDataObject
    {
        public string Text { get; set; }
        public string Description { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Argument { get; set; }
        public string UniqueId { get; set; }
        public int Order { get; set; }
        public bool Visible { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        [ForeignKey("Image")]
        public long? ImageId { get; set; }
        public Image Image { get; set; }
    }
}
