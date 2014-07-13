/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Data.SqlClient;
using System.Web;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel]
    public class EngineConfigurationModel
    {
        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.SafeMode)]
        public bool SafeMode { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.DisableSafeMode)]
        public bool DisableSafeMode { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.DatabaseConfiguration, Required = true)]
        public string Database { get; set; }

        [InputField(InputFieldType.Email, UserInterfaceTextIds.FatalErrorsEmail, Required = true)]
        public string FatalErrorsEmail { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.RecaptchaPublicKey)]
        public string RecaptchaPublicKey { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.RecaptchaPrivateKey)]
        public string RecaptchaPrivateKey { get; set; }

        [InputField(InputFieldType.Number, UserInterfaceTextIds.RecordsPerPage)]
        public int RecordsPerPage { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.AuthCookieName)]
        public string AuthCookieName { get; set; }

        [InputField(InputFieldType.Number, UserInterfaceTextIds.AuthCookieTimeout)]
        public int AuthCookieTimeout { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.AuthCookiePath)]
        public string AuthCookiePath { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.AutoRefresh)]
        public bool AutoRefresh { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.NoAdminName)]
        public string NoAdminName { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.LogLevel)]
        public string LogLevel { get; set; }

        [InputField(InputFieldType.Number, UserInterfaceTextIds.BackgroundJobInterval)]
        public int BackgroundJobInterval { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.DefaultHostName)]
        public string DefaultHostName { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.AdminLoginPage)]
        public string AdminLoginPage { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.AdminLoginAddress)]
        public string AdminLoginAddress { get; set; }

        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDataCache _dataCache;

        public EngineConfigurationModel(IEngineConfiguration engineConfiguration, IDataCache dataCache)
        {
            _engineConfiguration = engineConfiguration;
            _dataCache = dataCache;
        }

        public void Initialize()
        {
            SafeMode = _engineConfiguration.SafeMode;
            Database = _engineConfiguration.Database;
            FatalErrorsEmail = _engineConfiguration.FatalErrorsEmail;
            RecaptchaPublicKey = _engineConfiguration.RecaptchaPublicKey;
            RecaptchaPrivateKey = _engineConfiguration.RecaptchaPrivateKey;
            RecordsPerPage = _engineConfiguration.RecordsPerPage;
            AuthCookieName = _engineConfiguration.AuthCookieName;
            AuthCookiePath = _engineConfiguration.AuthCookiePath;
            AuthCookieTimeout = _engineConfiguration.AuthCookieTimeout;
            AutoRefresh = _engineConfiguration.AutoRefresh;
            NoAdminName = _engineConfiguration.NoAdminName;
            LogLevel = _engineConfiguration.LogLevel;
            BackgroundJobInterval = _engineConfiguration.BackgroundJobInterval;
            DefaultHostName = _engineConfiguration.DefaultHostName;
            AdminLoginPage = _engineConfiguration.AdminLoginPage;
            AdminLoginAddress = _engineConfiguration.AdminLoginAddress;
        }

        public void Save()
        {
            var databaseChanged = _engineConfiguration.Database != Database;
            var safeModeChanged = _engineConfiguration.SafeMode != SafeMode;

            if (databaseChanged)
            {
                using (var connection = new SqlConnection(Database))
                {
                    connection.Open();
                }
            }

            _engineConfiguration.SafeMode = SafeMode;
            _engineConfiguration.Database = Database;
            _engineConfiguration.FatalErrorsEmail = FatalErrorsEmail;
            _engineConfiguration.RecaptchaPublicKey = RecaptchaPublicKey;
            _engineConfiguration.RecaptchaPrivateKey = RecaptchaPrivateKey;
            _engineConfiguration.RecordsPerPage = RecordsPerPage;
            _engineConfiguration.AuthCookieName = AuthCookieName;
            _engineConfiguration.AuthCookiePath = AuthCookiePath;
            _engineConfiguration.AuthCookieTimeout = AuthCookieTimeout;
            _engineConfiguration.AutoRefresh = AutoRefresh;
            _engineConfiguration.NoAdminName = NoAdminName;
            _engineConfiguration.LogLevel = LogLevel;
            _engineConfiguration.BackgroundJobInterval = BackgroundJobInterval;
            _engineConfiguration.DefaultHostName = DefaultHostName;
            _engineConfiguration.AdminLoginPage = AdminLoginPage;
            _engineConfiguration.AdminLoginAddress = AdminLoginAddress;

            _engineConfiguration.Save();

            _dataCache.Reset();

            if (databaseChanged || safeModeChanged)
            {
                // Restart the app to reload database
                HttpRuntime.UnloadAppDomain();
            }
        }
    }
}
