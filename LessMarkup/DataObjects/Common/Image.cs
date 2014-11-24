/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class Image : DataObject
    {
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
        public string ThumbnailContentType { get; set; }
        public byte[] Thumbnail { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public long? UserId { get; set; }
        public string FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
