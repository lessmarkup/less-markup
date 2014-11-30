/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;
using LessMarkup.DataObjects.Security;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

namespace LessMarkup.Engine.Security
{
    class CurrentUser : ICurrentUser
    {
        #region Private Fields

        private const string AuthContextItem = "LessMarkupAuthUser";
        private const int TicketVersion = 1;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDomainModelProvider _domainModelProvider;

        #endregion

        public CurrentUser(IEngineConfiguration engineConfiguration, IDomainModelProvider domainModelProvider)
        {
            _engineConfiguration = engineConfiguration;
            _domainModelProvider = domainModelProvider;
        }

        #region Properties For User Context

        public static CookieUserModel ContextUser
        {
            get
            {
                if (!HasContextUser)
                {
                    return null;
                }

                return (CookieUserModel) HttpContext.Current.Items[AuthContextItem];
            }
            set
            {
                HttpContext.Current.Items[AuthContextItem] = value;
            }
        }

        public static bool HasContextUser
        {
            get { return HttpContext.Current.Items.Contains(AuthContextItem); }
            set
            {
                if (value == HasContextUser)
                {
                    return;
                }

                if (!value)
                {
                    HttpContext.Current.Items.Remove(AuthContextItem);
                }
                else
                {
                    ContextUser = null;
                }
            }
        }

        private CookieUserModel GetCurrentUser()
        {
            if (!HasContextUser)
            {
                MapCurrentUser();
            }

            return ContextUser;
        }

        #endregion

        public bool IsFakeUser
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.IsFakeUser;
            }
        }

        public void MapCurrentUser()
        {
            if (HasContextUser)
            {
                return;
            }

            var contextUser = CreateCurrentUser();

            if (contextUser != null)
            {
                var dataCache = DependencyResolver.Resolve<IDataCache>();

                if (!contextUser.IsAdministrator && !dataCache.Get<ISiteConfiguration>().HasUsers)
                {
                    this.LogDebug("Users functionality is disabled");
                    contextUser = null;
                }

            }

            ContextUser = contextUser;
        }

        public string Email
        {
            get
            {
                var user = GetCurrentUser();
                return user == null ? null : user.Email;
            }
        }

        public bool EmailConfirmed
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.EmailConfirmed;
            }
        }

        public string Name
        {
            get
            {
                var user = GetCurrentUser();
                return user == null ? null : user.Name;
            }
        }

        public long? UserId
        {
            get
            {
                var user = GetCurrentUser();
                return user == null ? (long?) null : user.UserId;
            }
        }

        public IReadOnlyList<long> Groups
        {
            get
            {
                var user = GetCurrentUser();
                return user == null ? null : user.Groups;
            }
        }

        public IReadOnlyDictionary<string, string> Properties
        {
            get
            {
                var user = GetCurrentUser();
                if (user == null || string.IsNullOrWhiteSpace(user.Properties))
                {
                    return null;
                }
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(user.Properties).ToDictionary(v => v.Key, v => v.Value != null ? v.Value.ToString() : "");
            }
        }

        public bool IsAdministrator
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.IsAdministrator;
            }
        }

        public bool IsApproved
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.IsApproved;
            }
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        private FormsAuthenticationTicket DecryptTicket(string value)
        {
            FormsAuthenticationTicket ticket;
            try
            {
                ticket = FormsAuthentication.Decrypt(value);
            }
            catch (Exception e)
            {
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                }
                this.LogDebug("Failed to decrypt user cookie, exception: " + e.Message);
                ContextUser = null;
                return null;
            }

            return ticket;
        }

        private CookieUserModel CreateCurrentUser()
        {
            var httpCookie = HttpContext.Current.Request.Cookies[_engineConfiguration.AuthCookieName];

            if (httpCookie == null || string.IsNullOrEmpty(httpCookie.Value))
            {
                this.LogDebug("User cookie is empty or not set");
                return null;
            }

            this.LogDebug("Trying to parse authentication ticket");

            var ticket = DecryptTicket(httpCookie.Value);

            long userId;

            if (ticket == null || ticket.Version != TicketVersion || !long.TryParse(ticket.UserData, out userId))
            {
                if (ticket == null)
                {
                    this.LogDebug("Ticket is null");
                }
                else if (ticket.Version != TicketVersion)
                {
                    this.LogDebug("Ticket has wrong version");
                }
                else
                {
                    this.LogDebug("Cannot parse ticket userdata");
                }

                return null;
            }

            if (userId == -1 && ticket.Name.Equals(_engineConfiguration.NoAdminName, StringComparison.InvariantCultureIgnoreCase))
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    if (NoGlobalAdminUser(domainModel))
                    {
                        this.LogDebug("No admin defined and username is equal to NoAdminName");
                        return new CookieUserModel
                        {
                            Email = ticket.Name,
                            Groups = new List<long>(),
                            IsAdministrator = true,
                            IsFakeUser = true,
                            IsApproved = true,
                            EmailConfirmed = true,
                            UserId = -1,
                        };
                    }
                }
            }

            var dataCache = DependencyResolver.Resolve<IDataCache>();

            var currentUser = dataCache.Get<IUserCache>(userId);

            if (currentUser.IsRemoved)
            {
                this.LogDebug("Cannot find user " + userId + " for current ticket");
                return null;
            }

            if (currentUser.IsBlocked)
            {
                if (!currentUser.UnblockTime.HasValue || currentUser.UnblockTime.Value >= DateTime.UtcNow)
                {
                    this.LogDebug("User is blocked");
                    return null;
                }
            }

            if (!currentUser.IsAdministrator && !dataCache.Get<ISiteConfiguration>().HasUsers)
            {
                this.LogDebug("Users functionality is disabled");
                return null;
            }

            if (!ticket.IsPersistent)
            {
                // Note: tickets use only local date/time

                if (ticket.Expiration < DateTime.Now)
                {
                    this.LogDebug("Ticket is expired");
                    ContextUser = null;
                    // Expiring the cookie
                    Logout();
                    return null;
                }

                var cookieTime = DateTime.Now;
                var cookieExpiration = DateTime.Now + TimeSpan.FromMinutes(_engineConfiguration.AuthCookieTimeout);

                ticket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, cookieTime, cookieExpiration, false, ticket.UserData);

                HttpContext.Current.Response.Cookies.Remove(_engineConfiguration.AuthCookieName);

                var cookie = new HttpCookie(_engineConfiguration.AuthCookieName, FormsAuthentication.Encrypt(ticket))
                {
                    HttpOnly = true,
                    Path = _engineConfiguration.AuthCookiePath,
                    Expires = cookieExpiration
                };

                HttpContext.Current.Response.Cookies.Add(cookie);
            }

            return new CookieUserModel
            {
                Email = currentUser.Email,
                Name = currentUser.Name,
                Groups = currentUser.Groups,
                IsAdministrator = currentUser.IsAdministrator,
                IsApproved = currentUser.IsApproved,
                EmailConfirmed = currentUser.EmailConfirmed,
                UserId = userId,
                Properties = currentUser.Properties,
            };
        }

        private bool LoginUser(string email, long userId, bool savePassword)
        {
            this.LogDebug("Logging in user " + email);

            // Note: tickets use only local time

            var cookieTime = DateTime.Now;
            var cookieExpiration = DateTime.Now + TimeSpan.FromMinutes(_engineConfiguration.AuthCookieTimeout);

            var ticket = new FormsAuthenticationTicket(TicketVersion, email, cookieTime, cookieExpiration, savePassword, userId.ToString(CultureInfo.InvariantCulture), _engineConfiguration.AuthCookiePath);
            var cookie = new HttpCookie(_engineConfiguration.AuthCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Path = _engineConfiguration.AuthCookiePath
            };

            if (!savePassword)
            {
                cookie.Expires = cookieExpiration;
            }

            HttpContext.Current.Response.Cookies.Add(cookie);

            HasContextUser = false;

            MapCurrentUser();

            return true;
        }

        public bool LoginWithOAuth(string provider, string providerUserId, bool savePassword, bool allowAdmin, bool allowRegular, string address)
        {
            this.LogDebug("Validating OAuth user");

            var dataCache = DependencyResolver.Resolve<IDataCache>();

            if (!allowAdmin && !dataCache.Get<ISiteConfiguration>().HasUsers)
            {
                this.LogDebug("Users functionality is disabled");
                return false;
            }

            using (var model = _domainModelProvider.Create())
            {
                var user = model.Query().From<User>().Where("AuthProvider = $ AND AuthProviderUserId = $", provider, providerUserId).FirstOrDefault<User>();

                if (user != null && user.IsBlocked)
                {
                    if (user.UnblockTime.HasValue && user.UnblockTime.Value < DateTime.UtcNow)
                    {
                        user.IsBlocked = false;
                        user.BlockReason = null;
                        user.UnblockTime = null;
                        model.Update(user);
                        DependencyResolver.Resolve<IChangeTracker>().AddChange<User>(user.Id, EntityChangeType.Updated, model);
                    }
                    else
                    {
                        user = null;
                    }
                }

                if (user == null)
                {
                    this.LogDebug("Cannot find valid user for authprovider '" + provider + "' and userid '" + providerUserId + "'");
                    return false;
                }

                if (user.IsAdministrator)
                {
                    if (!allowAdmin)
                    {
                        this.LogDebug("User not administrator, cancelling login");
                        return false;
                    }
                }
                else
                {
                    if (!allowRegular)
                    {
                        this.LogDebug("User is not administrator, cancelling login");
                        return false;
                    }
                }

                if (!LoginUser(user.Email, user.Id, savePassword))
                {
                    return false;
                }

                AddSuccessfulLoginHistory(address, model, user.Id);

                HasContextUser = false;

                return true;
            }
        }

        public bool LoginWithPassword(string email, string password, bool savePassword, bool allowAdmin, bool allowRegular, string address, string encodedPassword)
        {
            this.LogDebug("Validating user '" + email + "'");

            var dataCache = DependencyResolver.Resolve<IDataCache>();

            if (!allowAdmin && !dataCache.Get<ISiteConfiguration>().HasUsers)
            {
                this.LogDebug("Users functionality is disabled");
                return false;
            }

            if (!EmailCheck.IsValidEmail(email))
            {
                this.LogDebug("User '" + email + "' has invalid email");
                return false;
            }

            if (string.IsNullOrWhiteSpace(encodedPassword) && !TextValidator.CheckPassword(password))
            {
                this.LogDebug("Failed to pass password rules check");
                return false;
            }

            using (var model = _domainModelProvider.Create())
            {
                if (allowAdmin && email.Equals(_engineConfiguration.NoAdminName, StringComparison.InvariantCultureIgnoreCase) && NoGlobalAdminUser(model))
                {
                    this.LogDebug("No admin defined and user email is equal to NoAdminName");

                    if (!LoginUser(email, -1, false))
                    {
                        return false;
                    }

                    model.Create(new SuccessfulLoginHistory {Address = address, Time = DateTime.UtcNow, UserId = -2});
                    return true;
                }

                var user = model.Query().From<User>().Where("Email = $", email).FirstOrDefault<User>();

                if (user != null && user.IsBlocked)
                {
                    this.LogDebug("User is blocked");

                    if (!user.UnblockTime.HasValue || user.UnblockTime.Value >= DateTime.UtcNow)
                    {
                        return false;
                    }

                    this.LogDebug("Unblock time is arrived, unblocking the user");
                    user.IsBlocked = false;
                    user.BlockReason = null;
                    user.UnblockTime = null;
                    model.Update(user);
                    DependencyResolver.Resolve<IChangeTracker>().AddChange<User>(user.Id, EntityChangeType.Updated, model);
                }

                if (user == null)
                {
                    this.LogDebug("Cannot find user '" + email + "'");
                    return false;
                }

                if (!CheckPassword(user.Id, user.Password, user.Salt, user.IsBlocked, user.IsRemoved, user.RegistrationExpires, password, encodedPassword, address))
                {
                    this.LogDebug("User '" + email + "' failed password check");
                    return false;
                }

                if (user.IsAdministrator)
                {
                    if (!allowAdmin)
                    {
                        this.LogDebug("Expected admin but the user is not admin");
                        return false;
                    }
                }
                else
                {
                    if (!allowRegular)
                    {
                        this.LogDebug("Expected regular user but the user is admin");
                        return false;
                    }
                }

                if (!LoginUser(email, user.Id, savePassword))
                {
                    return false;
                }

                AddSuccessfulLoginHistory(address, model, user.Id);

                model.Update(user);

                HasContextUser = false;

                return true;
            }
        }

        public void Logout()
        {
            HttpContext.Current.Response.Cookies.Remove(_engineConfiguration.AuthCookieName);
            var cookie = new HttpCookie(_engineConfiguration.AuthCookieName, string.Empty)
            {
                Expires = new DateTime(2000, 1, 1),
                Path = _engineConfiguration.AuthCookiePath
            };

            HttpContext.Current.Response.Cookies.Add(cookie);

            ContextUser = null;
            HasContextUser = true;
        }

        private bool NoGlobalAdminUser(IDomainModel model)
        {
            var user = model.Query().From<User>().Where("IsAdministrator = $ AND IsRemoved = $ AND (IsBlocked = $ OR UnblockTime < $)", true, false, false, DateTime.Now).FirstOrDefault<User>("Id");
            return user == null;
        }

        private static void AddSuccessfulLoginHistory(string address, IDomainModel domainModel, long userId)
        {
            var history = new SuccessfulLoginHistory
            {
                Address = address,
                Time = DateTime.UtcNow,
                UserId = userId
            };

            domainModel.Create(history);

            var loginIpaddress = new UserLoginIpAddress
            {
                UserId = userId,
                Created = DateTime.UtcNow,
                IpAddress = HttpContext.Current.Request.UserHostAddress
            };

            domainModel.Create(loginIpaddress);
        }

        public void DeleteSelf(string password)
        {
            var currentUser = GetCurrentUser();

            if (currentUser == null)
            {
                throw new Exception("Cannot find user");
            }

            using (var model = _domainModelProvider.Create())
            {
                var user = model.Query().From<User>().Where("Id = $ AND IsRemoved = $", currentUser.UserId, false).FirstOrDefault<User>();

                if (user == null)
                {
                    throw new Exception("Cannot find user");
                }

                if (!CheckPassword(user.Id, user.Password, user.Salt, false, false, null, password, null, HttpContext.Current.Request.UserHostAddress))
                {
                    throw new Exception(LanguageHelper.GetText(DataFramework.Constants.ModuleType.MainModule, MainModuleTextIds.WrongUserPassword));
                }

                user.IsRemoved = true;
                model.Update(user);

                Logout();
            }
        }

        public bool CheckPassword(IDomainModel domainModel, string password, string address)
        {
            if (!UserId.HasValue)
            {
                return false;
            }

            var user = domainModel.Query().From<User>().Where("Id = $ AND IsRemoved = $", UserId.Value, false).FirstOrDefault<User>();

            if (user == null)
            {
                return false;
            }

            return CheckPassword(user.Id, user.Password, user.Salt, user.IsBlocked, user.IsRemoved, user.RegistrationExpires, password, null, address);
        }

        public Tuple<string, string> LoginHash(string email)
        {
            email = email.Trim();

            string hash1 = "";
            string hash2 = UserSecurity.GenerateSalt();

            if (!string.IsNullOrWhiteSpace(email))
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var user = domainModel.Query().From<User>().Where("Email = $ AND IsRemoved = $", email, false).FirstOrDefault<User>("Salt");
                        
                    if (user != null)
                    {
                        hash1 = user.Salt;
                    }
                }
            }

            using (var hashAlgorithm = HashAlgorithm.Create("SHA512"))
            {
                if (hashAlgorithm == null)
                {
                    throw new NotSupportedException();
                }
                if (string.IsNullOrWhiteSpace(hash1))
                {
                    hash1 = GenerateFakeSalt(hashAlgorithm, email);
                }

                return Tuple.Create(hash1, hash2);
            }
        }

        private static string GenerateFakeSalt(HashAlgorithm hashAlgorithm, string email)
        {
            var data = MachineKey.Protect(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(email)), null);
            return Convert.ToBase64String(data, 0, 16);
        }

        public bool CheckPassword(long? userId, string userPassword, string userSalt, bool isBlocked, bool isRemoved, DateTime? registrationExpires, string password, string encodedPassword, string remoteAddress)
        {
            if (userId.HasValue && (isBlocked || isRemoved))
            {
                this.LogDebug("User is null or blocked or removed");
                return false;
            }

            if (string.IsNullOrEmpty(remoteAddress))
            {
                this.LogDebug("User remote address is not specified");
                return false;
            }

            var timeLimit = DateTime.UtcNow - new TimeSpan(0, _engineConfiguration.FailedAttemptsRememberMinutes, 0);
            var maxAttemptCount = _engineConfiguration.MaximumFailedAttempts * 2;

            using (var model = _domainModelProvider.Create())
            {
                var failedAttempt = model.Query().From<FailedLoginHistory>().Where("UserId IS NULL AND Address = $", remoteAddress).FirstOrDefault<FailedLoginHistory>();

                if (failedAttempt != null && failedAttempt.LastAccess >= timeLimit && failedAttempt.AttemptCount >= maxAttemptCount)
                {
                    this.LogDebug("User is exceeded failed attempt limit for remote address '" + remoteAddress + "'");
                    failedAttempt.LastAccess = DateTime.UtcNow;
                    model.Update(failedAttempt);
                    return false;
                }

                if (!userId.HasValue)
                {
                    this.LogDebug("User is not found, logging failed attempt from address '" + remoteAddress + "'");
                    var isNew = false;
                    if (failedAttempt == null)
                    {
                        failedAttempt = new FailedLoginHistory { Address = remoteAddress, AttemptCount = 0, UserId = null };
                        isNew = true;
                    }
                    else if (failedAttempt.LastAccess < timeLimit)
                    {
                        failedAttempt.AttemptCount = 0;
                    }

                    failedAttempt.LastAccess = DateTime.UtcNow;
                    failedAttempt.AttemptCount++;
                    if (isNew)
                    {
                        model.Create(failedAttempt);
                    }
                    else
                    {
                        model.Update(failedAttempt);
                    }
                    return false;
                }

                if (registrationExpires.HasValue && DateTime.UtcNow >= registrationExpires.Value)
                {
                    this.LogDebug("User registration is expired, removing the user from users list");
                    var u = model.Query().From<User>().Where("Id = $", userId.Value).First<User>();
                    u.IsRemoved = true;
                    model.Update(u);
                    return false;
                }

                var addressFailedAttempt = failedAttempt;

                failedAttempt = model.Query().From<FailedLoginHistory>().Where("UserId = $", userId.Value).FirstOrDefault<FailedLoginHistory>();

                if (failedAttempt != null && failedAttempt.LastAccess >= timeLimit && failedAttempt.AttemptCount >= maxAttemptCount)
                {
                    this.LogDebug("Found failed attempts for specified user which exceed maximum attempt count");
                    failedAttempt.LastAccess = DateTime.UtcNow;
                    model.Update(failedAttempt);
                    return false;
                }

                bool passwordValid = userPassword == "-" && userSalt == "-";

                if (!passwordValid)
                {
                    if (!string.IsNullOrWhiteSpace(encodedPassword))
                    {
                        var pass1 = userPassword;
                        var split = encodedPassword.Split(new[] { ';' });
                        if (split.Length == 2 && !string.IsNullOrWhiteSpace(split[0]) && !string.IsNullOrWhiteSpace(split[1]))
                        {
                            var pass2 = UserSecurity.EncodePassword(pass1, split[0]);
                            passwordValid = pass2 == split[1];
                        }
                    }
                    else
                    {
                        if (UserSecurity.EncodePassword(password, userSalt) == userPassword)
                        {
                            passwordValid = true;
                        }
                    }
                }

                if (passwordValid)
                {
                    this.LogDebug("Password is recognized as valid");

                    if (failedAttempt != null || addressFailedAttempt != null)
                    {
                        if (failedAttempt != null)
                        {
                            model.Delete<FailedLoginHistory>(failedAttempt.Id);
                        }

                        if (addressFailedAttempt != null && addressFailedAttempt != failedAttempt)
                        {
                            model.Delete<FailedLoginHistory>(addressFailedAttempt.Id);
                        }
                    }

                    if (registrationExpires.HasValue)
                    {
                        var u = model.Query().From<User>().Where("Id = $", userId.Value).First<User>();
                        u.RegistrationExpires = null;
                        model.Update(u);
                    }

                    return true;
                }

                this.LogDebug("Password is invalid, logging new failed attempt for the user");

                var isNew1 = false;
                if (failedAttempt == null)
                {
                    failedAttempt = new FailedLoginHistory { Address = remoteAddress, UserId = userId.Value, AttemptCount = 0 };
                    isNew1 = true;
                }
                else if (failedAttempt.LastAccess < timeLimit)
                {
                    failedAttempt.AttemptCount = 0;
                }

                failedAttempt.LastAccess = DateTime.UtcNow;
                failedAttempt.AttemptCount++;
                if (isNew1)
                {
                    model.Create(failedAttempt);
                }
                else
                {
                    model.Update(failedAttempt);
                }
                return false;
            }
        }
    }
}
