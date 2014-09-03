/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Framework;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule.Model
{
    [RecordModel]
    public class SendContactModel : MailTemplateModel
    {
        private readonly IMailSender _mailSender;

        public SendContactModel(IMailSender mailSender)
        {
            _mailSender = mailSender;
        }

        [InputField(InputFieldType.RichText, ReadOnly = true)]
        public string Caption { get; set; }

        [InputField(InputFieldType.Email, MainModuleTextIds.YourEmail, Required = true)]
        public string Email { get; set; }

        [InputField(InputFieldType.MultiLineText, MainModuleTextIds.YourMessage, Required = true)]
        public string Message { get; set; }

        public void Submit()
        {
            _mailSender.SendMail(Email, UserEmail, "SendContact", this);
        }
    }
}
