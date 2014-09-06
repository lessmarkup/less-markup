using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Structure
{
    public interface INotificationProvider
    {
        string Title { get; }
        string Tooltip { get; }
        string Icon { get; }
        int GetValueChange(long? fromVersion, long? toVersion, IDomainModel domainModel);
    }
}
