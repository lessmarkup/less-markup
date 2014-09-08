/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Threading;
using System.Web;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
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
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISiteMapper _siteMapper;
        private readonly IMailSender _mailSender;

        public ForgotPasswordModel(IUserSecurity userSecurity, IDomainModelProvider domainModelProvider, ISiteMapper siteMapper, IMailSender mailSender)
        {
            _userSecurity = userSecurity;
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;
            _mailSender = mailSender;
        }

        public void Initialize()
        {
            Message = LanguageHelper.GetText(Constants.ModuleType.UserInterface,
                UserInterfaceTextIds.ForgotPasswordMessage);
        }

        public void Submit(INodeHandler nodeHandler, string fullPath)
        {
            var siteId = _siteMapper.SiteId;
            if (!siteId.HasValue)
            {
                return;
            }

            var hostName = HttpContext.Current.Request.Headers["host"];

            if (string.IsNullOrWhiteSpace(hostName))
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.GetCollection<DataObjects.Security.User>().FirstOrDefault(u => u.SiteId == siteId && u.Email == Email);

                if (user == null)
                {
                    Thread.Sleep(new Random(Environment.TickCount).Next(100, 500));
                    return;
                }

                var ticket = HttpUtility.UrlEncode(_userSecurity.CreatePasswordChangeToken(user.Id));

                _mailSender.SendMail(null, user.Id, Email, "ResetPassword", new ResetPasswordEmailModel
                {
                    ResetUrl = string.Format("http://{0}/{1}/ticket/{2}", hostName, fullPath.TrimStart(new []{'/'}), ticket),
                    SiteName = _siteMapper.Title,
                    HostName = hostName,
                    Subject = LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.RestorePassword)
                });
            }
        }
    }
}
