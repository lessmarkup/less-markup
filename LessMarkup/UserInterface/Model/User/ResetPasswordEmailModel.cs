/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.User
{
    public class ResetPasswordEmailModel : MailTemplateModel
    {
        public string ResetUrl { get; set; }

        public string SiteName { get; set; }

        public string HostName { get; set; }
    }
}
