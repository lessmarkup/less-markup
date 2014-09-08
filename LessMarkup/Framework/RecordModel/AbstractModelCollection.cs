/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Framework.RecordModel
{
    public abstract class AbstractModelCollection<T> : IModelCollection<T>
    {
        protected AbstractModelCollection(Type dataType)
        {
            CollectionId = AbstractDomainModel.GetCollectionIdVerified(dataType);
        } 

        public abstract IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder);
        public int CollectionId { get; private set; }
        public abstract IQueryable<T> Read(IDomainModel domainModel, List<long> ids);
        public abstract bool Filtered { get; }
        public abstract void Initialize(long? objectId, NodeAccessType nodeAccessType);
    }
}
