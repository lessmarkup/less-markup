/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;
using LessMarkup.DataObjects.Security;
using LessMarkup.DataObjects.User;
using LessMarkup.Engine.Configuration;
using LessMarkup.Engine.Language;
using LessMarkup.Engine.Logging;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Security
{
    class CurrentUser : ICurrentUser
    {
        #region Private Fields

        private const string AuthContextItem = "LessMarkupAuthUser";
        private const int TicketVersion = 1;
        private readonly ISiteMapper _siteMapper;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDomainModelProvider _domainModelProvider;

        #endregion

        public CurrentUser(ISiteMapper siteMapper, IEngineConfiguration engineConfiguration, IDomainModelProvider domainModelProvider)
        {
            _siteMapper = siteMapper;
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
            var ret = ContextUser;
            if (ret != null)
            {
                return ret;
            }
            MapCurrentUser();
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

                if (!contextUser.IsAdministrator && (!_siteMapper.SiteId.HasValue || !dataCache.Get<SiteConfigurationCache>().HasUsers))
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

        public bool IsAdministrator
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.IsAdministrator;
            }
        }

        public bool IsGlobalAdministrator
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.IsGlobalAdministrator;
            }
        }

        public bool IsValidated
        {
            get
            {
                var user = GetCurrentUser();
                return user != null && user.IsValidated;
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
                            IsGlobalAdministrator = true,
                            IsFakeUser = true,
                            IsValidated = true,
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

            if (!currentUser.IsAdministrator && _siteMapper.SiteId != currentUser.SiteId)
            {
                this.LogDebug("User site id is invalid");
                return null;
            }

            if (!currentUser.IsAdministrator && !dataCache.Get<SiteConfigurationCache>().HasUsers)
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
                Groups = currentUser.Groups,
                IsAdministrator = currentUser.IsAdministrator,
                IsGlobalAdministrator = currentUser.IsGlobalAdministrator,
                IsValidated = currentUser.IsValidated,
                UserId = userId,
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

        public bool LoginUserWithOAuth(string provider, string providerUserId, bool savePassword, bool allowAdmin, bool allowRegular, string address)
        {
            this.LogDebug("Validating OAuth user");

            var dataCache = DependencyResolver.Resolve<IDataCache>();

            if (!allowAdmin && (!_siteMapper.SiteId.HasValue || !dataCache.Get<SiteConfigurationCache>().HasUsers))
            {
                this.LogDebug("Users functionality is disabled");
                return false;
            }

            using (var model = _domainModelProvider.Create())
            {
                var siteId = _siteMapper.SiteId;

                var collection = model.GetCollection<User>().Where(u => u.AuthProvider == provider && u.AuthProviderUserId == providerUserId);

                collection = siteId.HasValue ?
                    collection.Where(u => (!u.SiteId.HasValue) || (u.SiteId.HasValue && u.SiteId == siteId)) :
                    collection.Where(u => !u.SiteId.HasValue);

                var user = collection.Include(u => u.Groups).SingleOrDefault();

                if (user != null && user.IsBlocked)
                {
                    if (user.UnblockTime.HasValue && user.UnblockTime.Value < DateTime.UtcNow)
                    {
                        user.IsBlocked = false;
                        user.BlockReason = null;
                        user.UnblockTime = null;

                        DependencyResolver.Resolve<IChangeTracker>().AddChange<User>(user.Id, EntityChangeType.Updated, model);

                        model.SaveChanges();
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

                model.SaveChanges();
            }

            return true;
        }

        public bool LoginUserWithPassword(string email, string password, bool savePassword, bool allowAdmin, bool allowRegular, string address, string encodedPassword)
        {
            this.LogDebug("Validating user '" + email + "'");

            var dataCache = DependencyResolver.Resolve<IDataCache>();

            if (!allowAdmin && (!_siteMapper.SiteId.HasValue || !dataCache.Get<SiteConfigurationCache>().HasUsers))
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
                var siteId = _siteMapper.SiteId;

                if (allowAdmin &&
                    email.Equals(_engineConfiguration.NoAdminName, StringComparison.InvariantCultureIgnoreCase) &&
                    NoGlobalAdminUser(model))
                {
                    this.LogDebug("No admin defined and user email is equal to NoAdminName");

                    if (!LoginUser(email, -1, false))
                    {
                        return false;
                    }

                    model.GetCollection<SuccessfulLoginHistory>()
                        .Add(new SuccessfulLoginHistory { Address = address, Time = DateTime.UtcNow, UserId = -2 });
                    model.SaveChanges();
                    return true;
                }

                var collection = model.GetCollection<User>().Where(u => u.Email == email);

                if (allowRegular && allowAdmin)
                {
                    if (siteId.HasValue)
                    {
                        collection = collection.Where(u => (u.SiteId.HasValue && u.SiteId == siteId) || !u.SiteId.HasValue);
                    }
                }
                else if (allowRegular)
                {
                    collection = collection.Where(u => u.SiteId.HasValue && u.SiteId == siteId);
                }

                var user = collection.Include(u => u.Groups).SingleOrDefault();

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

                    DependencyResolver.Resolve<IChangeTracker>().AddChange<User>(user.Id, EntityChangeType.Updated, model);

                    model.SaveChanges();
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

                if (!user.IsValidated && user.PasswordAutoGenerated)
                {
                    user.IsValidated = true;
                }

                AddSuccessfulLoginHistory(address, model, user.Id);

                model.SaveChanges();
            }

            return true;
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
        }

        private bool NoGlobalAdminUser(IDomainModel model)
        {
            return !model.GetCollection<User>().Any(u => u.IsAdministrator && !u.SiteId.HasValue && !u.IsRemoved && (!u.IsBlocked || (u.UnblockTime.HasValue && u.UnblockTime.Value < DateTime.UtcNow)));
        }

        private static void AddSuccessfulLoginHistory(string address, IDomainModel domainModel, long userId)
        {
            domainModel.GetCollection<SuccessfulLoginHistory>().Add(new SuccessfulLoginHistory
            {
                Address = address,
                Time = DateTime.UtcNow,
                UserId = userId
            });
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
                var user = model.GetCollection<User>().SingleOrDefault(u => u.Id == currentUser.UserId && !u.IsRemoved);

                if (user == null)
                {
                    throw new Exception("Cannot find user");
                }

                if (!CheckPassword(user.Id, user.Password, user.Salt, false, false, null, password, null, HttpContext.Current.Request.UserHostAddress))
                {
                    throw new Exception(LanguageHelper.GetText(DataFramework.Constants.ModuleType.MainModule, MainModuleTextIds.WrongUserPassword));
                }

                user.IsRemoved = true;

                model.SaveChanges();

                Logout();
            }
        }

        public bool CheckPassword(IDomainModel domainModel, string password, string address)
        {
            if (!UserId.HasValue)
            {
                return false;
            }

            var user = domainModel.GetCollection<User>().SingleOrDefault(u => u.Id == UserId.Value && u.SiteId == _siteMapper.SiteId && !u.IsRemoved);

            if (user == null)
            {
                return false;
            }

            return CheckPassword(user.Id, user.Password, user.Salt, user.IsBlocked, user.IsRemoved, user.RegistrationExpires, password, null, address);
        }

        public Tuple<string, string> LoginHash(string email)
        {
            email = email.Trim();

            var siteId = _siteMapper.SiteId;

            string hash1 = "";
            string hash2 = UserSecurity.GenerateSalt();

            if (!string.IsNullOrWhiteSpace(email))
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var user = siteId.HasValue ?
                        domainModel.GetCollection<User>().SingleOrDefault(u => u.Email == email && u.SiteId == siteId.Value) :
                        domainModel.GetCollection<User>().SingleOrDefault(u => u.Email == email && !u.SiteId.HasValue);

                    if (user == null && siteId.HasValue)
                    {
                        user = domainModel.GetCollection<User>().SingleOrDefault(u => u.Email == email && !u.SiteId.HasValue);
                    }

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
                var failedAttempt = model.GetCollection<FailedLoginHistory>().SingleOrDefault(f => !f.UserId.HasValue && f.Address == remoteAddress);

                if (failedAttempt != null && failedAttempt.LastAccess >= timeLimit && failedAttempt.AttemptCount >= maxAttemptCount)
                {
                    this.LogDebug("User is exceeded failed attempt limit for remote address '" + remoteAddress + "'");
                    failedAttempt.LastAccess = DateTime.UtcNow;
                    model.SaveChanges();
                    return false;
                }

                if (!userId.HasValue)
                {
                    this.LogDebug("User is not found, logging failed attempt from address '" + remoteAddress + "'");
                    if (failedAttempt == null)
                    {
                        failedAttempt = new FailedLoginHistory { Address = remoteAddress, AttemptCount = 0, UserId = null };
                        model.GetCollection<FailedLoginHistory>().Add(failedAttempt);
                    }
                    else if (failedAttempt.LastAccess < timeLimit)
                    {
                        failedAttempt.AttemptCount = 0;
                    }

                    failedAttempt.LastAccess = DateTime.UtcNow;
                    failedAttempt.AttemptCount++;
                    model.SaveChanges();
                    return false;
                }

                if (registrationExpires.HasValue && DateTime.UtcNow >= registrationExpires.Value)
                {
                    this.LogDebug("User registration is expired, removing the user from users list");
                    var u = model.GetCollection<User>().Single(u1 => u1.Id == userId.Value);
                    u.IsRemoved = true;
                    model.SaveChanges();
                    return false;
                }

                var addressFailedAttempt = failedAttempt;

                failedAttempt = model.GetCollection<FailedLoginHistory>().SingleOrDefault(f => f.UserId.HasValue && f.UserId.Value == userId.Value);

                if (failedAttempt != null && failedAttempt.LastAccess >= timeLimit && failedAttempt.AttemptCount >= maxAttemptCount)
                {
                    this.LogDebug("Found failed attempts for specified user which exceed maximum attempt count");
                    failedAttempt.LastAccess = DateTime.UtcNow;
                    model.SaveChanges();
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
                            model.GetCollection<FailedLoginHistory>().Remove(failedAttempt);
                        }
                        if (addressFailedAttempt != null && addressFailedAttempt != failedAttempt)
                        {
                            model.GetCollection<FailedLoginHistory>().Remove(addressFailedAttempt);
                        }
                    }

                    if (registrationExpires.HasValue)
                    {
                        var u = model.GetCollection<User>().Single(u1 => u1.Id == userId.Value);
                        u.RegistrationExpires = null;
                    }

                    model.SaveChanges();

                    return true;
                }

                this.LogDebug("Password is invalid, logging new failed attempt for the user");

                if (failedAttempt == null)
                {
                    failedAttempt = new FailedLoginHistory { Address = remoteAddress, UserId = userId.Value, AttemptCount = 0 };
                    model.GetCollection<FailedLoginHistory>().Add(failedAttempt);
                }
                else if (failedAttempt.LastAccess < timeLimit)
                {
                    failedAttempt.AttemptCount = 0;
                }

                failedAttempt.LastAccess = DateTime.UtcNow;
                failedAttempt.AttemptCount++;
                model.SaveChanges();
                return false;
            }
        }
    }
}
