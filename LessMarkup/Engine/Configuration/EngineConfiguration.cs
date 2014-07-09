/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LessMarkup.DataFramework;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.System;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace LessMarkup.Engine.Configuration
{
    class EngineConfiguration : IEngineConfiguration
    {
        private readonly bool _isCloudEnvironment;
        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
        private readonly object _syncConfiguration = new object();
        private DateTime _lastConfigurationLoad;

        public EngineConfiguration()
        {
            try
            {
                _isCloudEnvironment = RoleEnvironment.IsAvailable;
            }
            catch (Exception e)
            {
                this.LogException(e);
            }

            LoadFileConfiguration();
        }

        private static string FileConfigurationPath
        {
            get
            {
                return Path.Combine(SpecialFolder.ApplicationDataFolder, "Engine.config");
            }
        }

        private void LoadFileConfiguration()
        {
            if (_isCloudEnvironment)
            {
                return;
            }

            lock (_syncConfiguration)
            {
                _lastConfigurationLoad = DateTime.UtcNow;

                var configurationPath = FileConfigurationPath;

                FileConfiguration fileConfiguration = null;
                _properties.Clear();

                try
                {
                    if (File.Exists(configurationPath))
                    {
                        using (var reader = File.OpenRead(configurationPath))
                        {
                            fileConfiguration = (FileConfiguration) new XmlSerializer(typeof (FileConfiguration)).Deserialize(reader);
                        }
                    }

                    if (fileConfiguration != null && fileConfiguration.Properties != null)
                    {
                        foreach (var property in fileConfiguration.Properties)
                        {
                            _properties[property.Name] = property.Value;
                        }
                    }

                    var logLevel = LogLevel;
                    if (!string.IsNullOrWhiteSpace(logLevel))
                    {
                        LogLevel level;
                        LoggingHelper.Level = Enum.TryParse(logLevel, true, out level) ? level : Logging.LogLevel.None;
                    }
                    else
                    {
                        LoggingHelper.Level = Logging.LogLevel.None;
                    }
                }
                catch (Exception e)
                {
                    this.LogException(e);
                }
            }
        }

        private string GetProperty(string name, string defaultValue = "")
        {
            string ret;
            if (_isCloudEnvironment)
            {
                ret = CloudConfigurationManager.GetSetting(name);
                return !string.IsNullOrWhiteSpace(ret) ? ret : defaultValue;
            }

            lock (_syncConfiguration)
            {
                if ((DateTime.UtcNow - _lastConfigurationLoad).TotalMinutes > Constants.Engine.CheckConfigurationChangeMinutes)
                {
                    LoadFileConfiguration();
                }

                if (_properties.TryGetValue(name, out ret))
                {
                    return ret;
                }
            }

            return defaultValue;
        }

        private void SetProperty(string name, string value)
        {
            if (_isCloudEnvironment)
            {
                throw new UnauthorizedAccessException("Cannot change engine properties in cloud environments");
            }

            lock (_syncConfiguration)
            {
                _properties[name] = value;
            }
        }

        public string Database
        {
            get
            {
                return GetProperty("Database");
            }
            set
            {
                SetProperty("Database", value);
            }
        }

        public bool DisableSafeMode
        {
            get
            {
                return bool.Parse(GetProperty("DisableSafeMode", false.ToString()));
            }
            set
            {
                SetProperty("DisableSafeMode", value.ToString());
            }
        }

        public bool SafeMode
        {
            get
            {
                return bool.Parse(GetProperty("SafeMode", false.ToString()));
            }
            set
            {
                SetProperty("SafeMode", value.ToString());
            }
        }

        public bool DisableWebConfiguration
        {
            get
            {
                if (_isCloudEnvironment)
                {
                    return true;
                }
                return bool.Parse(GetProperty("DisableWebConfiguration", false.ToString()));
            }
            set
            {
                if (_isCloudEnvironment)
                {
                    return;
                }
                SetProperty("DisableWebConfiguration", value.ToString());
            }
        }

        public string FatalErrorsEmail
        {
            get
            {
                return GetProperty("FatalErrorsEmail");
            }
            set
            {
                SetProperty("FatalErrorsEmail", value);
            }
        }

        public bool SmtpConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SmtpServer);
            }
        }

        public int SmtpPort
        {
            get
            {
                return int.Parse(GetProperty("SmtpPort", 25.ToString(CultureInfo.InvariantCulture)));
            }
            set
            {
                SetProperty("SmtpPort", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public string SmtpServer
        {
            get
            {
                return GetProperty("SmtpServer");
            }
            set
            {
                SetProperty("SmtpServer", value);
            }
        }

        public string SmtpUsername
        {
            get
            {
                return GetProperty("SmtpUsername");
            }
            set
            {
                SetProperty("SmtpUsername", value);
            }
        }

        public string SmtpPassword
        {
            get
            {
                return GetProperty("SmtpPassword");
            }
            set
            {
                SetProperty("SmtpPassword", value);
            }
        }

        public bool SmtpSsl
        {
            get
            {
                return bool.Parse(GetProperty("SmtpSsl", false.ToString()));
            }
            set
            {
                SetProperty("SmtpSsl", value.ToString());
            }
        }

        public string DebugCompilationPath
        {
            get
            {
                return GetProperty("DebugCompilationPath");
            }
            set
            {
                SetProperty("DebugCompilationPath", value);
            }
        }

        public string RecaptchaPublicKey
        {
            get
            {
                return GetProperty("RecaptchaPublicKey");
            }
            set
            {
                SetProperty("RecaptchaPublicKey", value);
            }
        }

        public string RecaptchaPrivateKey
        {
            get
            {
                return GetProperty("RecaptchaPrivateKey");
            }
            set
            {
                SetProperty("RecaptchaPrivateKey", value);
            }
        }

        public bool UseTestMail
        {
            get
            {
                return bool.Parse(GetProperty("UseTestMail", false.ToString()));
            }
            set
            {
                SetProperty("UseTestMail", value.ToString());
            }
        }

        public string NoReplyEmail
        {
            get
            {
                var ret = GetProperty("NoReplyEmail");
                return string.IsNullOrWhiteSpace(ret) ? "no@reply.email" : ret;
            }
            set
            {
                SetProperty("NoReplyEmail", value);
            }
        }

        public string NoReplyName
        {
            get
            {
                return GetProperty("NoReplyName");
            }
            set
            {
                SetProperty("NoReplyName", value);
            }
        }

        public int FailedAttemptsRememberMinutes
        {
            get
            {
                return int.Parse(GetProperty("FailedAttemptsRememberMinutes", 15.ToString(CultureInfo.InvariantCulture)));
            }
            set
            {
                SetProperty("FailedAttemptsRememberMinutes", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public int MaximumFailedAttempts
        {
            get
            {
                return int.Parse(GetProperty("MaximumFailedAttempts", 5.ToString(CultureInfo.InvariantCulture)));
            }
            set
            {
                SetProperty("MaximumFailedAttempts", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public int RecordsPerPage
        {
            get
            {
                return int.Parse(GetProperty("RecordsPerPage", 10.ToString(CultureInfo.InvariantCulture)));
            }
            set
            {
                SetProperty("RecordsPerPage", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public string AuthCookieName
        {
            get
            {
                return GetProperty("AuthCookieName", "LessMarkup_Auth");
            }
            set
            {
                SetProperty("AuthCookieName", value);
            }
        }

        public int AuthCookieTimeout
        {
            get
            {
                return int.Parse(GetProperty("AuthCookieTimeout", 15.ToString(CultureInfo.InvariantCulture)));
            }
            set
            {
                SetProperty("AuthCookieTimeout", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public string AuthCookiePath
        {
            get
            {
                return GetProperty("AuthCookiePath", "/");
            }
            set
            {
                SetProperty("AuthCookiePath", value);
            }
        }

        public bool AutoRefresh
        {
            get
            {
                return bool.Parse(GetProperty("AutoRefresh", true.ToString()));
            }
            set
            {
                SetProperty("AutoRefresh", value.ToString());
            }
        }

        public bool UseChangeTracking
        {
            get
            {
                return bool.Parse(GetProperty("UseChangeTracking", true.ToString()));
            }
            set
            {
                SetProperty("UseChangeTracking", value.ToString());
            }
        }

        public string NoAdminName
        {
            get
            {
                return GetProperty("NoAdminName", "noadmin@noadmin.com");
            }
            set
            {
                SetProperty("NoAdminName", value);
            }
        }

        public string LogLevel
        {
            get
            {
                return GetProperty("LogLevel", "Error");
            }
            set
            {
                SetProperty("LogLevel", value);
            }
        }

        public int BackgroundJobInterval
        {
            get
            {
                return int.Parse(GetProperty("BackgroundJobInterval", 10.ToString(CultureInfo.InvariantCulture)));
            }
            set
            {
                SetProperty("BackgroundJobInterval", value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public string DefaultHostName
        {
            get
            {
                return GetProperty("DefaultHostName");
            }
            set
            {
            }
        }

        public string AdminLoginPage
        {
            get
            {
                return GetProperty("AdminLoginPage", "Login");
            }
            set
            {
                SetProperty("AdminLoginPage", value);
            }
        }

        public string AdminLoginAddress
        {
            get
            {
                return GetProperty("AdminLoginAddress");
            }
            set
            {
                SetProperty("AdminLoginAddress", value);
            }
        }

        public DateTime? LastDatabaseUpdate
        {
            get
            {
                var property = GetProperty("LastDatabaseUpdate");
                if (string.IsNullOrWhiteSpace(property))
                {
                    return null;
                }
                return new DateTime(long.Parse(property), DateTimeKind.Utc);
            }
            set
            {
                SetProperty("LastDatabaseUpdate", value.HasValue ? value.Value.Ticks.ToString(CultureInfo.InvariantCulture) : "");
            }
        }

        public bool IsCloudEnvironment
        {
            get
            {
                return _isCloudEnvironment;
            }
        }

        public string ModuleSearchPath
        {
            get
            {
                return GetProperty("ModuleSearchPath");
            }
            set
            {
                SetProperty("ModuleSearchPath", value);
            }
        }

        public void Save()
        {
            lock (_syncConfiguration)
            {
                try
                {
                    var fileConfiguration = new FileConfiguration
                    {
                        Properties = _properties.Where(p => !string.IsNullOrWhiteSpace(p.Value))
                            .Select(p => new FileConfigurationProperty
                            {
                                Name = p.Key,
                                Value = p.Value
                            }).ToList()
                    };

                    File.Delete(FileConfigurationPath);

                    using (var writer = File.OpenWrite(FileConfigurationPath))
                    {
                        new XmlSerializer(typeof (FileConfiguration)).Serialize(writer, fileConfiguration);
                    }
                }
                catch (Exception e)
                {
                    this.LogException(e);
                    throw;
                }
            }
        }
    }
}
