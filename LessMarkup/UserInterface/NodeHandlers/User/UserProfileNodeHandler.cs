/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Reflection;
using LessMarkup.Engine.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.User
{
    public class UserProfileNodeHandler : TabPageNodeHandler
    {
        public UserProfileNodeHandler(IDataCache dataCache, ICurrentUser currentUser, IModuleProvider moduleProvider) : base(dataCache, currentUser)
        {
            foreach (var module in moduleProvider.Modules)
            {
                foreach (var type in module.Assembly.GetTypes())
                {
                    if (!typeof (INodeHandler).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var handlerAttribute = type.GetCustomAttribute<UserProfileHandlerAttribute>();

                    if (handlerAttribute == null)
                    {
                        continue;
                    }

                    AddPage(type, LanguageHelper.GetText(module.ModuleType, handlerAttribute.TitleTextId));
                }
            }
        }
    }
}
