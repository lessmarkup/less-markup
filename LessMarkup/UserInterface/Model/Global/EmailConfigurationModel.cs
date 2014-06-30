/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel]
    public class EmailConfigurationModel
    {
        private readonly IEngineConfiguration _engineConfiguration;

        public EmailConfigurationModel(IEngineConfiguration engineConfiguration)
        {
            _engineConfiguration = engineConfiguration;
        }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.SmtpServer)]
        public string SmtpServer { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.SmtpUsername)]
        public string SmtpUsername { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.SmtpPassword)]
        public string SmtpPassword { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.SmtpUseSsl)]
        public bool SmtpUseSsl { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.UseTestMail)]
        public bool UseTestMail { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.NoReplyEmail)]
        public string NoReplyEmail { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.NoReplyName)]
        public string NoReplyName { get; set; }

        public void Initialize()
        {
            SmtpServer = _engineConfiguration.SmtpServer;
            SmtpUsername = _engineConfiguration.SmtpUsername;
            SmtpPassword = _engineConfiguration.SmtpPassword;
            SmtpUseSsl = _engineConfiguration.SmtpSsl;
            UseTestMail = _engineConfiguration.UseTestMail;
            NoReplyEmail = _engineConfiguration.NoReplyEmail;
            NoReplyName = _engineConfiguration.NoReplyName;
        }

        public void Save()
        {
            _engineConfiguration.SmtpServer = SmtpServer;
            _engineConfiguration.SmtpUsername = SmtpUsername;
            _engineConfiguration.SmtpPassword = SmtpPassword;
            _engineConfiguration.SmtpSsl = SmtpUseSsl;
            _engineConfiguration.UseTestMail = UseTestMail;
            _engineConfiguration.NoReplyEmail = NoReplyEmail;
            _engineConfiguration.NoReplyName = NoReplyName;
            _engineConfiguration.Save();
        }
    }
}
