/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Threading;
using System.Web;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.ForgotPassword, SubmitWithCaptcha = true)]
    public class ForgotPasswordModel
    {
        [InputField(InputFieldType.Label)]
        public string Message { get; set; }

        [InputField(InputFieldType.Email, UserInterfaceTextIds.Email)]
        public string Email { get; set; }

        private readonly IUserSecurity _userSecurity;
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IMailSender _mailSender;

        public ForgotPasswordModel(IUserSecurity userSecurity, IDomainModelProvider domainModelProvider, IMailSender mailSender, IDataCache dataCache)
        {
            _userSecurity = userSecurity;
            _domainModelProvider = domainModelProvider;
            _mailSender = mailSender;
            _dataCache = dataCache;
        }

        public void Initialize()
        {
            Message = LanguageHelper.GetText(Constants.ModuleType.UserInterface,
                UserInterfaceTextIds.ForgotPasswordMessage);
        }

        public void Submit(INodeHandler nodeHandler, string fullPath)
        {
            var hostName = HttpContext.Current.Request.Headers["host"];

            if (string.IsNullOrWhiteSpace(hostName))
            {
                return;
            }

            var siteName = _dataCache.Get<ISiteConfiguration>().SiteName;

            if (string.IsNullOrWhiteSpace(siteName))
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.Query().From<DataObjects.Security.User>().Where("Email = $", Email).FirstOrDefault<DataObjects.Security.User>();

                if (user == null)
                {
                    Thread.Sleep(new Random(Environment.TickCount).Next(100, 500));
                    return;
                }

                var ticket = HttpUtility.UrlEncode(user.Email + "/" + _userSecurity.CreatePasswordChangeToken(user.Id));

                _mailSender.SendMail(null, user.Id, Email, "ResetPassword", new ResetPasswordEmailModel
                {
                    ResetUrl = string.Format("http://{0}/{1}/ticket/{2}", hostName, fullPath.TrimStart(new []{'/'}), ticket),
                    SiteName = _dataCache.Get<ISiteConfiguration>().SiteName,
                    HostName = hostName,
                    Subject = LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.RestorePassword)
                });
            }
        }
    }
}
