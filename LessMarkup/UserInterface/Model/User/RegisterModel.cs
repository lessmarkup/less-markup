/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web;
using LessMarkup.Engine.Configuration;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.Register, SubmitWithCaptcha = true)]
    public class RegisterModel
    {
        private readonly IDataCache _dataCache;
        private readonly IUserSecurity _userSecurity;
        private readonly ICurrentUser _currentUser;

        public RegisterModel(IDataCache dataCache, IUserSecurity userSecurity, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _userSecurity = userSecurity;
            _currentUser = currentUser;
        }

        [InputField(InputFieldType.Email, UserInterfaceTextIds.Email, Required = true)]
        public string Email { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Name)]
        public string Name { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.GeneratePassword)]
        public bool GeneratePassword { get; set; }

        [InputField(InputFieldType.PasswordRepeat, UserInterfaceTextIds.Password, Required = true, VisibleCondition = "!GeneratePassword")]
        public string Password { get; set; }

        public bool ShowUserAgreement { get; set; }

        [InputField(InputFieldType.RichText, UserInterfaceTextIds.UserAgreement, VisibleCondition = "ShowUserAgreement")]
        public string UserAgreement { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.Agree, Required = true, VisibleCondition = "ShowUserAgreement")]
        public bool Agree { get; set; }

        public object GetRegisterObject()
        {
            var siteProperties = _dataCache.Get<SiteConfigurationCache>();

            if (!siteProperties.HasUsers)
            {
                throw new Exception("Cannot register new user");
            }

            UserAgreement = siteProperties.UserAgreement;
            ShowUserAgreement = !string.IsNullOrWhiteSpace(UserAgreement);

            var modelCache = _dataCache.Get<IRecordModelCache>();

            return new
            {
                RegisterObject = this,
                ModelId = modelCache.GetDefinition<RegisterModel>().Id
            };
        }

        public object Register(System.Web.Mvc.Controller controller, string properties)
        {
            var siteProperties = _dataCache.Get<SiteConfigurationCache>();

            if (!siteProperties.HasUsers)
            {
                throw new Exception("Cannot register new user");
            }

            var modelCache = _dataCache.Get<IRecordModelCache>();
            var definition = modelCache.GetDefinition(typeof (RegisterModel));
            definition.ValidateInput(this, true, properties);

            var address = HttpContext.Current.Request.UserHostAddress;

            _userSecurity.CreateUser(Name, Password, Email, address, controller.Url, false, false);

            var loggedIn = _currentUser.LoginUserWithPassword(Email, Password, false, false, true, address, null);

            return new
            {
                UserName = Name,
                ShowConfiguration = false,
                UserLoggedIn = loggedIn
            };
        }
    }
}
