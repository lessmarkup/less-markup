/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IFilteredCollection<in TR>
    {
        IQueryable<int> ReadIdsFiltered(IDomainModel domainModel, object filter);
    }
}
