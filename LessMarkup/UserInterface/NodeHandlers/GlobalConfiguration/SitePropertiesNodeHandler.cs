using System;
using LessMarkup.Engine.Configuration;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.SiteProperties)]
    public class SitePropertiesNodeHandler : DialogNodeHandler<SiteConfigurationCache>, IRecordNodeHandler
    {
        private long? _siteId;

        protected override SiteConfigurationCache LoadObject()
        {
            var cache = Interfaces.DependencyResolver.Resolve<SiteConfigurationCache>();
            DateTime? expirationTime;
            cache.Initialize(_siteId, out expirationTime, null);
            return cache;
        }

        protected override string SaveObject(SiteConfigurationCache changedObject)
        {
            changedObject.Save(_siteId);
            return null;
        }

        public void Initialize(long recordId)
        {
            _siteId = recordId;
        }
    }
}
