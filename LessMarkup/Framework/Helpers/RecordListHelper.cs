/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Framework.Helpers
{
    public static class RecordListHelper
    {
        public static string PageLink(string baseUrl, int page)
        {
            return page == 1 ? baseUrl : string.Format("{0}?p={1}", baseUrl, page);
        }

        public static string LastPageLink(string baseUrl)
        {
            return string.Format("{0}?p=last", baseUrl);
        }
    }
}
