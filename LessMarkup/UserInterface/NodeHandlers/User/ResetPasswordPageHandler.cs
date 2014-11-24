/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.UserInterface.Model.User;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.User
{
    public class ResetPasswordPageHandler : DialogNodeHandler<ChangePasswordModel>
    {
        private string _ticket;

        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IUserSecurity _userSecurity;

        public ResetPasswordPageHandler(ILightDomainModelProvider domainModelProvider, IUserSecurity userSecurity, IDataCache dataCache) : base(dataCache)
        {
            _domainModelProvider = domainModelProvider;
            _userSecurity = userSecurity;
        }

        public void Initialize(string ticket)
        {
            _ticket = ticket;
        }

        protected override ActionResult CreateResult(string path)
        {
            if (path != null)
            {
                return null;
            }

            var userId = _userSecurity.ValidatePasswordChangeToken(_ticket);

            if (!userId.HasValue)
            {
                return new HttpNotFoundResult();
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                if (domainModel.Query().From<DataObjects.Security.User>().FindOrDefault<DataObjects.Security.User>(userId.Value) == null)
                {
                    return new HttpNotFoundResult();
                }
            }

            return null;
        }

        protected override ChangePasswordModel LoadObject()
        {
            return Interfaces.DependencyResolver.Resolve<ChangePasswordModel>();
        }

        protected override string SaveObject(ChangePasswordModel changedObject)
        {
            var userId = _userSecurity.ValidatePasswordChangeToken(_ticket);

            if (!userId.HasValue)
            {
                this.LogDebug("Cannot change password: cannot get valid user id");
                return LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.PasswordChangeError);
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.Query().FindOrDefault<DataObjects.Security.User>(userId.Value);

                if (user == null)
                {
                    this.LogDebug("Cannot change password: user id=" + userId.Value + " does not exist");
                    return LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.PasswordChangeError);
                }

                string salt;
                string encodedPassword;
                _userSecurity.ChangePassword(changedObject.Password, out salt, out encodedPassword);

                user.Password = encodedPassword;
                user.Salt = salt;
                user.LastPasswordChanged = DateTime.UtcNow;
                user.EmailConfirmed = true;

                domainModel.Update(user);

                return LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.PasswordChanged);
            }
        }
    }
}
