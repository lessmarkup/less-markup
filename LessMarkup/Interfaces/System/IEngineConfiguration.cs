/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.System
{
    public interface IEngineConfiguration
    {
        string Database { get; set; }
        bool DisableSafeMode { get; set; }
        bool SafeMode { get; set; }
        bool DisableWebConfiguration { get; set; }
        string FatalErrorsEmail { get; set; }
        bool SmtpConfigured { get; }
        string SmtpServer { get; set; }
        string SmtpUsername { get; set; }
        string SmtpPassword { get; set; }
        bool SmtpSsl { get; set; }
        string DebugCompilationPath { get; set; }
        string RecaptchaPublicKey { get; set; }
        string RecaptchaPrivateKey { get; set; }
        bool UseTestMail { get; set; }
        string NoReplyEmail { get; set; }
        string NoReplyName { get; set; }
        int FailedAttemptsRememberMinutes { get; set; }
        int MaximumFailedAttempts { get; set; }
        int RecordsPerPage { get; set; }
        string AuthCookieName { get; set; }
        int AuthCookieTimeout { get; set; }
        string AuthCookiePath { get; set; }
        bool IsCloudEnvironment { get; }
        void Save();
        bool AutoRefresh { get; set; }
        string NoAdminName { get; set; }
        string LogLevel { get; set; }
        int BackgroundJobInterval { get; set; }
        string DefaultHostName { get; set; }
        string AdminLoginPage { get; set; }
        string AdminLoginAddress { get; set; }
        bool UseChangeTracking { get; set; }
        DateTime? LastDatabaseUpdate { get; set; }
        string ModuleSearchPath { get; set; }
        bool MigrateDataLossAllowed { get; set; }
        bool DisableCustomizations { get; set; }
    }
}
