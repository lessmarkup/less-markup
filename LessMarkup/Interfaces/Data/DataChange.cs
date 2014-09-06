using System;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Data
{
    public interface IDataChange
    {
        long Id { get; }
        long EntityId { get; }
        DateTime Created { get; }
        EntityChangeType Type { get; }
        long? UserId { get; }
        long Parameter1 { get; }
        long Parameter2 { get; }
        long Parameter3 { get; }
    }
}
