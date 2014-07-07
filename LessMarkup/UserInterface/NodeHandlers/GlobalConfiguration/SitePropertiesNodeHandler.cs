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
            cache.Initialize(_siteId);
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
