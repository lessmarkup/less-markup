using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.User;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.User
{
    public class ForgotPasswordPageHandler : DialogNodeHandler<ForgotPasswordModel>
    {
        public ForgotPasswordPageHandler(IDataCache dataCache) : base(dataCache)
        {
        }

        protected override string ApplyCaption
        {
            get { return LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.RestorePassword); }
        }

        protected override ForgotPasswordModel LoadObject()
        {
            var model = DependencyResolver.Resolve<ForgotPasswordModel>();
            model.Initialize();
            return model;
        }

        protected override string SaveObject(ForgotPasswordModel changedObject)
        {
            changedObject.Submit(this, FullPath);
            return LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.RestorePasswordRequestSent);
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            var parts = path.Split(new[] {'/'});

            if (parts.Length != 2 || parts[0] != "ticket")
            {
                return null;
            }

            var handler = DependencyResolver.Resolve<ResetPasswordPageHandler>();

            ((INodeHandler) handler).Initialize(null, null, null, null, null, NodeAccessType.Read);

            handler.Initialize(parts[1]);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Path = path
            };
        }
    }
}
