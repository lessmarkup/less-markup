/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Text
{
    public class SearchResult
    {
        public int CollectionId { get; set; }
        public long EntityId { get; set; }
        public string Text { get; set; }
    }
}
