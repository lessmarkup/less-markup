using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Configuration;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.Configuration
{
    [ConfigurationHandler(UserInterfaceTextIds.UserProperties)]
    public class UserPropertiesNodeHandler : RecordListLinkNodeHandler<UserPropertyModel>
    {
        public UserPropertiesNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser) : base(domainModelProvider, dataCache, currentUser)
        {
        }
    }
}
