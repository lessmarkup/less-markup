using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Module
{
    public interface IEntitySearch
    {
        string ValidateAndGetUrl(int collectionId, long entityId, IDomainModel domainModel);
        string GetFriendlyName(int collectionId);
    }
}
