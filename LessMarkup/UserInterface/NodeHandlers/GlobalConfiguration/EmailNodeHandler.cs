/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.EmailConfiguration, IsGlobal = true)]
    public class EmailNodeHandler : DialogNodeHandler<EmailConfigurationModel>
    {
        protected override EmailConfigurationModel LoadObject(object settings)
        {
            var model = DependencyResolver.Resolve<EmailConfigurationModel>();
            model.Initialize();
            return model;
        }

        protected override string SaveObject(EmailConfigurationModel changedObject)
        {
            changedObject.Save();
            return null;
        }
    }
}
