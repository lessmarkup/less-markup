/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web;
using System.Web.Mvc;
using LessMarkup.Framework.Configuration;
using LessMarkup.Framework.Site;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.UserInterface.Model.RecordModel;
using LessMarkup.UserInterface.Model.Structure;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.Register)]
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

        [InputField(InputFieldType.Password, UserInterfaceTextIds.Password, Required = true)]
        public string Password { get; set; }

        public bool ShowUserAgreement { get; set; }

        [InputField(InputFieldType.RichText, UserInterfaceTextIds.UserAgreement, VisibleCondition = "ShowUserAgreement")]
        public string UserAgreement { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.Agree, Required = true, VisibleCondition = "ShowUserAgreement")]
        public bool Agree { get; set; }

        public object GetRegisterObject()
        {
            UserAgreement = _dataCache.Get<SiteConfigurationCache>().UserAgreement;
            ShowUserAgreement = !string.IsNullOrWhiteSpace(UserAgreement);
            return this;
        }

        public object Register(System.Web.Mvc.Controller controller)
        {
            var modelCache = _dataCache.Get<RecordModelCache>();
            var definition = modelCache.GetDefinition(typeof (RegisterModel));
            definition.ValidateInput(this, true);

            var address = HttpContext.Current.Request.UserHostAddress;

            _userSecurity.CreateUser(Name, Password, Email, address, x => controller.Url.Action("NodeEntryPoint", "Node", new { path = string.Format("{0}?validate={1}", JsonEntryPointModel.ValidateUserPath, x)}));

            var loggedIn = _currentUser.LoginUserWithPassword(Email, Password, false, false, true, address, null);

            return new
            {
                UserName = Name,
                ShowConfiguration = false,
                UserLoggedIn = loggedIn
            };
        }

        public ActionResult ValidateSecret()
        {
            var secret = HttpContext.Current.Request.QueryString["validate"];

            var success = !string.IsNullOrWhiteSpace(secret) && _userSecurity.ConfirmUser(secret);

            return new ContentResult
            {
                Content = success ? "You have successfully validated user e-mail." : "Wrong request",
                ContentType = "text/plain"
            };
        }
    }
}
