/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mail;
using LessMarkup.DataObjects.Common;
using LessMarkup.DataObjects.User;
using LessMarkup.Framework.Configuration;
using LessMarkup.Framework.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;
using MailMessage = System.Net.Mail.MailMessage;

// We use here deprecated version of Microsoft mail sender as it supports normal SSL processing
#pragma warning disable 0618

namespace LessMarkup.Framework.Email
{
    public class MailSender : IMailSender
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IMailTemplateProvider _mailTemplateProvider;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDataCache _dataCache;

        public MailSender(IDomainModelProvider domainModelProvider, IMailTemplateProvider mailTemplateProvider, IEngineConfiguration engineConfiguration, IDataCache dataCache)
        {
            _domainModelProvider = domainModelProvider;
            _mailTemplateProvider = mailTemplateProvider;
            _engineConfiguration = engineConfiguration;
            _dataCache = dataCache;
        }

        private string NoReplyEmail
        {
            get
            {
                var ret = _dataCache.Get<SiteConfigurationCache>().NoReplyEmail;
                if (string.IsNullOrWhiteSpace(ret))
                {
                    ret = _engineConfiguration.NoReplyEmail;
                }
                if (string.IsNullOrWhiteSpace(ret))
                {
                    ret = "no@reply.email";
                }
                return ret;
            }
        }

        private string NoReplyName
        {
            get
            {
                var ret = _dataCache.Get<SiteConfigurationCache>().NoReplyName;
                if (string.IsNullOrWhiteSpace(ret))
                {
                    ret = _engineConfiguration.NoReplyName;
                }
                return ret;
            }
        }

        private static string ComposeAddress(string email, string name)
        {
            return string.Format("\"{0}\" <{1}>", name, email);
        }

        public void SendMail<T>(string smtpServer, string smtpUser, string smtpPassword, bool smtpSsl, string emailFrom, string userEmailTo, string templateId, T parameters) where T : MailTemplateModel
        {
            string body;
            string subject;
            try
            {
                parameters.UserEmail = userEmailTo;

                body = _mailTemplateProvider.ExecuteTemplate(templateId, parameters);
                subject = parameters.Subject;
            }
            catch (Exception e)
            {
                this.LogException(e);
                throw new Exception("Cannot send mail");
            }

            SendMail("", emailFrom, parameters.UserName, userEmailTo, subject, body, templateId, smtpServer, smtpUser, smtpPassword, smtpSsl);
        }

        public void SendMail<T>(string emailFrom, string emailTo, string templateId, T parameters) where T : MailTemplateModel
        {
            SendMail(_engineConfiguration.SmtpServer, _engineConfiguration.SmtpUsername,
                _engineConfiguration.SmtpPassword, _engineConfiguration.SmtpSsl, emailFrom, emailTo, templateId, parameters);
        }

        public void SendMail<T>(long? userIdFrom, long? userIdTo, string userEmailTo, string templateId, T parameters) where T : MailTemplateModel
        {
            try
            {
                User userFrom = null;
                User userTo = null;

                using (var domainModel = _domainModelProvider.Create())
                {
                    if (userIdTo.HasValue)
                    {
                        userTo = domainModel.GetCollection<User>().Single(u => u.UserId == userIdTo);
                    }
                    if (userIdFrom.HasValue)
                    {
                        userFrom = domainModel.GetCollection<User>().Single(u => u.UserId == userIdFrom.Value);
                    }
                }

                string fromName, fromEmail;

                if (userFrom != null)
                {
                    fromEmail = userFrom.ShowEmail ? userFrom.Email : NoReplyEmail;
                    fromName = userFrom.Name;
                }
                else
                {
                    fromEmail = NoReplyEmail;
                    fromName = NoReplyName;
                }

                if (userTo != null)
                {
                    parameters.UserEmail = userTo.Email;
                    parameters.UserName = userTo.Name;
                }
                else if (!string.IsNullOrWhiteSpace(userEmailTo))
                {
                    parameters.UserEmail = userEmailTo;
                }
                else
                {
                    throw new Exception("UserIdTo is not specified");
                }

                var body = _mailTemplateProvider.ExecuteTemplate(templateId, parameters);
                var subject = parameters.Subject;

                if (string.IsNullOrWhiteSpace(fromEmail))
                {
                    throw new Exception("From email is not specified, cannot send email for template '" + templateId + "'");
                }

                SendMail(fromName, fromEmail, parameters.UserName, parameters.UserEmail, subject, body, templateId,
                    _engineConfiguration.SmtpServer, _engineConfiguration.SmtpUsername, _engineConfiguration.SmtpPassword, _engineConfiguration.SmtpSsl);
            }
            catch (Exception e)
            {
                this.LogException(e);
                throw new Exception("Cannot send mail");
            }
        }

        public void SendPlainEmail(string emailTo, string subject, string body)
        {
            try
            {
                using (var client = CreateClient(_engineConfiguration.SmtpServer, _engineConfiguration.SmtpUsername, _engineConfiguration.SmtpPassword, _engineConfiguration.SmtpSsl))
                using (var message = CreateMessage(NoReplyName, NoReplyEmail, "", emailTo))
                {
                    message.Body = body;
                    message.Subject = subject;
                    message.IsBodyHtml = true;

                    client.Send(message);
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
                throw new Exception("Cannot send mail");
            }
        }

        private static SmtpClient CreateClient(string server, string username, string password, bool useSsl)
        {
            int port = 25;
            int pos = server.IndexOf(':');
            if (pos > 0)
            {
                port = int.Parse(server.Substring(pos + 1));
                server = server.Remove(pos).Trim();
            }

            var client = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = useSsl
            };

            return client;
        }

        private static MailMessage CreateMessage(string fromName, string fromEmail, string toName, string toEmail)
        {
            var message = new MailMessage();
            message.To.Add(new MailAddress(toEmail, toName));
            message.From = new MailAddress(fromEmail, fromName);
            return message;
        }

        private const string SmtpServerPropertyName = "http://schemas.microsoft.com/cdo/configuration/smtpserver";
        private const string SmtpServerPortPropertyName = "http://schemas.microsoft.com/cdo/configuration/smtpserverport";
        private const string SendUsingPropertyName = "http://schemas.microsoft.com/cdo/configuration/sendusing";
        private const string SmtpUseSslPropertyName = "http://schemas.microsoft.com/cdo/configuration/smtpusessl";
        private const string SmtpAuthenticatePropertyName = "http://schemas.microsoft.com/cdo/configuration/smtpauthenticate";
        private const string SendUsernamePropertyName = "http://schemas.microsoft.com/cdo/configuration/sendusername";
        private const string SendPasswordPropertyName = "http://schemas.microsoft.com/cdo/configuration/sendpassword";

        private void SendMail(string fromName, string fromAddress, string toName, string toAddress, string subject, string body, string templateId,
            string server, string username, string password, bool useSsl)
        {
            if (_engineConfiguration.UseTestMail)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var testEmail = domainModel.GetSiteCollection<TestMail>().Create();
                    testEmail.Body = body;
                    testEmail.From = ComposeAddress(fromAddress, fromName);
                    testEmail.Template = templateId;
                    testEmail.Sent = DateTime.UtcNow;
                    testEmail.Subject = subject;
                    testEmail.To = ComposeAddress(toAddress, toName);
                    testEmail.Views = 0;

                    domainModel.GetSiteCollection<TestMail>().Add(testEmail);
                    domainModel.SaveChanges();
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(server))
            {
                throw new Exception("SMTP server is not specified");
            }

            if (useSsl)
            {
// ReSharper disable once CSharpWarnings::CS0618
                var mail = new System.Web.Mail.MailMessage();

                int port = 465;
                int pos = server.IndexOf(':');
                if (pos > 0)
                {
                    port = int.Parse(server.Substring(pos + 1));
                    server = server.Remove(pos).Trim();
                }
                    
                mail.Fields[SmtpServerPropertyName] = server;
                mail.Fields[SmtpServerPortPropertyName] = port;
                mail.Fields[SendUsingPropertyName] = 2;
                mail.Fields[SmtpUseSslPropertyName] = true;
                mail.Fields[SmtpAuthenticatePropertyName] = 1;
                mail.Fields[SendUsernamePropertyName] = username;
                mail.Fields[SendPasswordPropertyName] = password;
                if (!string.IsNullOrWhiteSpace(fromName))
                {
                    mail.From = "\"" + fromName + "\" <" + fromAddress + ">";
                }
                else
                {
                    mail.From = fromAddress;
                }
                if (!string.IsNullOrWhiteSpace(toName))
                {
                    mail.To = "\"" + toName + "\" <" + toAddress + ">";
                }
                else
                {
                    mail.To = toAddress;
                }
// ReSharper disable once CSharpWarnings::CS0618
                mail.BodyFormat = MailFormat.Html;
                mail.Subject = subject;
                mail.Body = body;

// ReSharper disable once CSharpWarnings::CS0618
                SmtpMail.Send(mail);
            }
            else
            {
                using (var message = CreateMessage(fromName, fromAddress, toName, toAddress))
                {
                    message.Body = body;
                    message.Subject = subject;
                    message.IsBodyHtml = true;

                    using (var client = CreateClient(server, username, password, false))
                    {
                        client.Send(message);
                    }
                }
            }
        }
    }
}
