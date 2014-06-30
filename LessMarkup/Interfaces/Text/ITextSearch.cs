/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Text
{
    public interface ITextSearch
    {
        List<SearchResult> Search(string text, int startRecord, int recordCount, IDomainModel domainModel);
    }
}
