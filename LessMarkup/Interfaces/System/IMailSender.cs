/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.System
{
    public interface IMailSender
    {
        void SendMail<T>(string smtpServer, string smtpUser, string smtpPassword, bool smtpSsl, string emailFrom, string emailTo, string viewPath, T parameters) where T : MailTemplateModel;
        void SendMail<T>(string emailFrom, string emailTo, string viewPath, T parameters) where T : MailTemplateModel;
        void SendMail<T>(long? userIdFrom, long? userIdTo, string userEmailTo, string viewPath, T parameters) where T : MailTemplateModel;
        void SendPlainEmail(string emailTo, string subject, string message);
    }
}
