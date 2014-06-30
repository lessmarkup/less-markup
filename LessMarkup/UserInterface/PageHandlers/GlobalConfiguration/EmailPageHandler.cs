/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.DataFramework;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.PageHandlers.Common;

namespace LessMarkup.UserInterface.PageHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.EmailConfiguration, IsGlobal = true)]
    public class EmailPageHandler : DialogPageHandler<EmailConfigurationModel>
    {
        protected override EmailConfigurationModel LoadObject()
        {
            var model = DependencyResolver.Resolve<EmailConfigurationModel>();
            model.Initialize();
            return model;
        }

        protected override void SaveObject(EmailConfigurationModel changedObject)
        {
            changedObject.Save();
        }
    }
}
