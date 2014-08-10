using LessMarkup.DataFramework;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.Framework.Helpers
{
    public static class UserHelper
    {
        public static string GetUserProfileLink(long userId)
        {
            var currentUser = DependencyResolver.Resolve<ICurrentUser>();

            if (currentUser.UserId.HasValue && currentUser.UserId.Value == userId)
            {
                return "/" + Constants.NodePath.Profile;
            }

            return string.Format("/{0}/{1}", Constants.NodePath.UserCards, userId);
        }
    }
}
