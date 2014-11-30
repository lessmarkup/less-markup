/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
            if (path == null)
            {
                return null;
            }

            var pos = path.IndexOf('/');
            if (pos <= 0)
            {
                return null;
            }

            if (path.Substring(0, pos) != "ticket")
            {
                return null;
            }

            var handler = DependencyResolver.Resolve<ResetPasswordPageHandler>();

            ((INodeHandler) handler).Initialize(null, null, null, null, null, NodeAccessType.Read);

            var ticket = path.Substring(pos + 1).Split(new []{'/'});

            if (ticket.Length != 2)
            {
                return null;
            }

            handler.Initialize(ticket[0], ticket[1]);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Path = path
            };
        }
    }
}
