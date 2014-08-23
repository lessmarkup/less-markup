using LessMarkup.Engine.Structure;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.SiteProperties)]
    public class SitePropertiesNodeHandler : DialogNodeHandler<SitePropertiesModel>
    {
        protected override SitePropertiesModel LoadObject()
        {
            var model = DependencyResolver.Resolve<SitePropertiesModel>();
            model.Initialize(ObjectId);
            return model;
        }

        protected override string SaveObject(SitePropertiesModel changedObject)
        {
            changedObject.Save(ObjectId);
            return null;
        }
    }
}
