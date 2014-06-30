/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class LoginModel
    {
        private readonly ICurrentUser _currentUser;

        public LoginModel(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
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

            if (!_currentUser.LoginUserWithPassword(userName, "", savePassword != null && savePassword == true.ToString(), true, true,
                HttpContext.Current.Request.UserHostAddress, passwordHash))
            {
                throw new UnauthorizedAccessException("User not found or wrong password");
            }

            return new
            {
                UserName = _currentUser.Email,
                ShowConfiguration = _currentUser.IsAdministrator
            };
        }

        public object HandleLogout()
        {
            _currentUser.Logout();
            return new {};
        }
    }
}
