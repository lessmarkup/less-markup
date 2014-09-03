/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Engine.Language;
using LessMarkup.Framework;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Structure;
using LessMarkup.MainModule.Model;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.MainModule.NodeHandlers
{
    [UserProfileHandler(MainModuleTextIds.UserCommon)]
    public class UserProfileCommonNodeHandler : DialogNodeHandler<UserProfileModel>
    {
        public UserProfileCommonNodeHandler(IDataCache dataCache) : base(dataCache)
        {
        }

        protected override UserProfileModel LoadObject()
        {
            var model = Interfaces.DependencyResolver.Resolve<UserProfileModel>();
            model.Initialize();
            return model;
        }

        protected override string SaveObject(UserProfileModel changedObject)
        {
            changedObject.Save();
            return null;
        }
    }
}
