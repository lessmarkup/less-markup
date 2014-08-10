using System;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IRecordModelCache : ICacheHandler
    {
        IRecordModelDefinition GetDefinition<T>();
        IRecordModelDefinition GetDefinition(Type type);
        IRecordModelDefinition GetDefinition(string id);
        bool HasDefinition(Type type);
    }
}
