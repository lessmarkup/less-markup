/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Framework.Helpers
{
    public static class TextHelper
    {
        public static string ToJsonCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Substring(0, 1).ToLower() + value.Substring(1);
        }

        public static string FromJsonCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Substring(0, 1).ToUpper() + value.Substring(1);
        }
    }
}
