/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.DataFramework;

namespace LessMarkup.MainModule.Initialization
{
    public class UserSearch : IEntitySearch
    {
        public string ValidateAndGetUrl(int collectionId, long entityId, IDomainModel domainModel)
        {
            return UserHelper.GetUserProfileLink(entityId);
        }

        public string GetFriendlyName(int collectionId)
        {
            return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.UserName);
        }
    }
}
