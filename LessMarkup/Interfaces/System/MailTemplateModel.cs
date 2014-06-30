/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.System
{
    public class MailTemplateModel
    {
        public string Subject { get; set; }

        public string UserEmail { get; set; }

        public string UserName { get; set; }
    }
}
