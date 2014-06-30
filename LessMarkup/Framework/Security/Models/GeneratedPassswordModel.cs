/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Security.Models
{
    public class GeneratedPassswordModel : MailTemplateModel
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string SiteName { get; set; }
        public string SiteLink { get; set; }
    }
}
