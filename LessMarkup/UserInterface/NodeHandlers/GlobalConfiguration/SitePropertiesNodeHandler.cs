/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Engine.Structure;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.SiteProperties)]
    public class SitePropertiesNodeHandler : DialogNodeHandler<SitePropertiesModel>
    {
        public SitePropertiesNodeHandler(IDataCache dataCache) : base(dataCache)
        {
        }

        protected override SitePropertiesModel LoadObject()
        {
            var model = DependencyResolver.Resolve<SitePropertiesModel>();
            model.Initialize();
            return model;
        }

        protected override string SaveObject(SitePropertiesModel changedObject)
        {
            changedObject.Save();
            return null;
        }
    }
}
