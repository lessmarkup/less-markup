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
        private string _email;

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IUserSecurity _userSecurity;

        public ResetPasswordPageHandler(IDomainModelProvider domainModelProvider, IUserSecurity userSecurity, IDataCache dataCache) : base(dataCache)
        {
            _domainModelProvider = domainModelProvider;
            _userSecurity = userSecurity;
        }

        public void Initialize(string email, string ticket)
        {
            _ticket = ticket;
            _email = email;
        }

        protected override ActionResult CreateResult(string path)
        {
            if (path != null)
            {
                return null;
            }

            var userId = _userSecurity.ValidatePasswordChangeToken(_email, _ticket);

            if (!userId.HasValue)
            {
                return new HttpNotFoundResult();
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.Query().From<DataObjects.Security.User>().Where("Email = $ AND PasswordChangeToken = $", _email, _ticket).FirstOrDefault<DataObjects.Security.User>();

                if (user == null || !user.PasswordChangeTokenExpires.HasValue ||
                    user.PasswordChangeTokenExpires.Value >= DateTime.UtcNow)
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
            var userId = _userSecurity.ValidatePasswordChangeToken(_email, _ticket);

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

                user.PasswordChangeToken = null;
                user.PasswordChangeTokenExpires = null;
                user.Password = encodedPassword;
                user.EmailConfirmed = true;
                user.Salt = salt;
                user.LastPasswordChanged = DateTime.UtcNow;
                user.EmailConfirmed = true;

                domainModel.Update(user);

                return LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.PasswordChanged);
            }
        }
    }
}
