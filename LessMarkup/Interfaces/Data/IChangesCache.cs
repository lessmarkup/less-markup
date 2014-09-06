using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Data
{
    public interface IChangesCache : ICacheHandler
    {
        IEnumerable<IDataChange> GetCollectionChanges(int collectionId, long? fromId, long? toId, Func<IDataChange, bool> filterFunc = null);
        long? LastChangeId { get; }
    }
}
