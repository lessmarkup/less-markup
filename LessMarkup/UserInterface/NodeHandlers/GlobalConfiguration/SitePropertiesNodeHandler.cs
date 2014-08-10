﻿using LessMarkup.Engine.Configuration;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.SiteProperties)]
    public class SitePropertiesNodeHandler : DialogNodeHandler<SiteConfigurationCache>
    {
        protected override SiteConfigurationCache LoadObject()
        {
            var cache = Interfaces.DependencyResolver.Resolve<SiteConfigurationCache>();
            cache.Initialize(ObjectId);
            return cache;
        }

        protected override string SaveObject(SiteConfigurationCache changedObject)
        {
            changedObject.Save(ObjectId);
            return null;
        }
    }
}
