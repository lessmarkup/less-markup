using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.User
{
    public class UserCardNodeHandler : TabPageNodeHandler
    {
        public UserCardNodeHandler(IDataCache dataCache, ICurrentUser currentUser) : base(dataCache, currentUser)
        {
        }
    }
}
