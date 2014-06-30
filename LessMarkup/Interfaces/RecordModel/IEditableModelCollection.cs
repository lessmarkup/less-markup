/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IEditableModelCollection<TR> : IModelCollection<TR>
    {
        TR AddRecord(TR record, bool returnObject);
        TR UpdateRecord(TR record, bool returnObject);
        bool DeleteRecords(IEnumerable<long> recordIds);
    }
}
