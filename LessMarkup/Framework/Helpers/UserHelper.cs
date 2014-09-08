/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
