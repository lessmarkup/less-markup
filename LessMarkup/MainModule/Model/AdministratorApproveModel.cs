/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule.Model
{
    public class AdministratorApproveModel : MailTemplateModel
    {
        public string ConfirmLink { get; set; }
        public string BlockLink { get; set; }
        public string Email { get; set; }
        public long UserId { get; set; }
    }
}
