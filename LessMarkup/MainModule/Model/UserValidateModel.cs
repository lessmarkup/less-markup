/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule.Model
{
    public class UserValidateModel
    {
        private readonly IUserSecurity _userSecurity;
        private readonly IDataCache _dataCache;
        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IMailSender _mailSender;

        public bool Success { get; set; }
        public bool ApproveRequired { get; set; }

        public UserValidateModel(IUserSecurity userSecurity, IDataCache dataCache, ILightDomainModelProvider domainModelProvider, IMailSender mailSender)
        {
            _userSecurity = userSecurity;
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
            _mailSender = mailSender;
        }

        public void ValidateSecret(string secret, UrlHelper urlHelper)
        {
            ApproveRequired = _dataCache.Get<ISiteConfiguration>().AdminApproveNewUsers;

            if (string.IsNullOrWhiteSpace(secret))
            {
                Success = false;
                return;
            }

            long userId;
            Success = _userSecurity.ConfirmUser(secret, out userId);

            if (!Success)
            {
                return;
            }

            if (ApproveRequired && Success)
            {
                var approveModel = new AdministratorApproveModel
                {
                    UserId = userId
                };

                IReadOnlyCollection<long> administrators;

                using (var domainModel = _domainModelProvider.Create())
                {
                    var user = domainModel.Query().Find<User>(userId);

                    approveModel.UserName = user.Name;
                    approveModel.Email = user.Email;

                    administrators =
                        domainModel.Query()
                            .From<User>()
                            .Where("IsAdministrator = $ AND IsRemoved = $ AND IsBlocked = $", true, false, false)
                            .ToIdList();
                }

                approveModel.ConfirmLink = urlHelper.Action("Approve", "Account", new { secret = _userSecurity.EncryptObject(new ApproveRequestModel
                {
                    IsApprove = true,
                    UserId = userId
                }) });

                approveModel.BlockLink = urlHelper.Action("Approve", "Account", new
                {
                    secret = _userSecurity.EncryptObject(new ApproveRequestModel
                    {
                        IsApprove = false,
                        UserId = userId
                    })
                });

                foreach (var admin in administrators)
                {
                    _mailSender.SendMail(null, admin, null, Constants.MailTemplates.ApproveUserRegistration, approveModel);
                }
            }
        }
    }
}
