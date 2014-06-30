/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.DataFramework;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.PageHandlers.Common;

namespace LessMarkup.UserInterface.PageHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.EngineConfiguration, IsGlobal = true)]
    public class EnginePageHandler : DialogPageHandler<EngineConfigurationModel>
    {
        protected override EngineConfigurationModel LoadObject()
        {
            var model = DependencyResolver.Resolve<EngineConfigurationModel>();
            model.Initialize();
            return model;
        }

        protected override void SaveObject(EngineConfigurationModel changedObject)
        {
            changedObject.Save();
        }
    }
}
