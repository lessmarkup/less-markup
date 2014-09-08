/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Framework.Helpers
{
    public static class ImageHelper
    {
        public static string ImageUrl(long imageId)
        {
            return string.Format("/Image/Get/{0}", imageId);
        }

        public static string ThumbnailUrl(long imageId)
        {
            return string.Format("/Image/Thumbnail/{0}", imageId);
        }

        public static string SmileUrl(string code)
        {
            return string.Format("/Image/Smile/{0}", code);
        }
    }
}
