/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using LessMarkup.Engine.Configuration;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.User
{
    public class LoginModel
    {
        private readonly ICurrentUser _currentUser;
        private readonly ISiteMapper _siteMapper;
        private readonly IDataCache _dataCache;
        private readonly IEngineConfiguration _engineConfiguration;

        public LoginModel(ICurrentUser currentUser, ISiteMapper siteMapper, IDataCache dataCache, IEngineConfiguration engineConfiguration)
        {
            _currentUser = currentUser;
            _siteMapper = siteMapper;
            _dataCache = dataCache;
            _engineConfiguration = engineConfiguration;
        }

        public object HandleStage1Request(Dictionary<string, string> data)
        {
            Thread.Sleep(new Random(Environment.TickCount).Next(30));
            var loginHash = _currentUser.LoginHash(data["user"]);

            return new
            {
                Pass1 = loginHash.Item1,
                Pass2 = loginHash.Item2,
            };
        }

        public object HandleStage2Request(Dictionary<string, string> data)
        {
            var userName = data["user"];
            var passwordHash = data["hash"];
            var savePassword = data["remember"];

            string administratorKey;

            if (!data.TryGetValue("administratorKey", out administratorKey))
            {
                administratorKey = "";
            }

            string adminLoginPage;

            if (_siteMapper.SiteId.HasValue)
            {
                var siteConfiguration = _dataCache.Get<SiteConfigurationCache>();
                adminLoginPage = siteConfiguration.AdminLoginPage;
            }
            else
            {
                adminLoginPage = _engineConfiguration.AdminLoginPage;
            }

            bool allowAdministrator = string.IsNullOrWhiteSpace(adminLoginPage) || administratorKey == adminLoginPage;

            bool allowUser = string.IsNullOrWhiteSpace(adminLoginPage);

            if (!_currentUser.LoginUserWithPassword(userName, "", savePassword != null && savePassword == true.ToString(), allowAdministrator, allowUser,
                HttpContext.Current.Request.UserHostAddress, passwordHash))
            {
                throw new UnauthorizedAccessException("User not found or wrong password");
            }

            return new
            {
                UserName = _currentUser.Email,
                ShowConfiguration = _currentUser.IsAdministrator,
                Path = string.IsNullOrWhiteSpace(adminLoginPage) ? "" : "/"
            };
        }

        public object HandleLogout()
        {
            _currentUser.Logout();
            return new {};
        }
    }
}
