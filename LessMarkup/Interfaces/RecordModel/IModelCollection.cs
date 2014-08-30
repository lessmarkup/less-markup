﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IModelCollection<out TR>
    {
        IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder);
        int CollectionId { get; }
        IQueryable<TR> Read(IDomainModel domainModel, List<long> ids);
        void Initialize(long? objectId, NodeAccessType nodeAccessType);
    }
}
