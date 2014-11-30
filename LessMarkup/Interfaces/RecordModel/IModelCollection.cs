/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IModelCollection<out TR>
    {
        IReadOnlyCollection<long> ReadIds(IQueryBuilder query, bool ignoreOrder);
        int CollectionId { get; }
        IReadOnlyCollection<TR> Read(IQueryBuilder queryBuilder, List<long> ids);
        void Initialize(long? objectId, NodeAccessType nodeAccessType);
    }
}
