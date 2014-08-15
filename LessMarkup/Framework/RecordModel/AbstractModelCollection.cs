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
            CollectionId = AbstractDomainModel.GetCollectionId(dataType);
        } 

        public abstract IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder);
        public int CollectionId { get; private set; }
        public abstract IQueryable<T> Read(IDomainModel domainModel, List<long> ids);
        public abstract bool Filtered { get; }
        public abstract void Initialize(long? objectId, NodeAccessType nodeAccessType);
    }
}
