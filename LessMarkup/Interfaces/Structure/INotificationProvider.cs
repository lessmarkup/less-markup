using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Structure
{
    public interface INotificationProvider
    {
        string Title { get; }
        string Tooltip { get; }
        string Icon { get; }
        long? Version { get; }
        Tuple<int, long?> GetCountAndVersion(long? lastVersion, IDomainModel domainModel);
    }
}
