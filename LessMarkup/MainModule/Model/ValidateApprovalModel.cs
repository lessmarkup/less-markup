/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule.Model
{
    public class ValidateApprovalModel
    {
        private readonly IUserSecurity _userSecurity;
        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly IMailSender _mailSender;
        private readonly IDataCache _dataCache;

        public bool Success { get; set; }

        public ValidateApprovalModel(IUserSecurity userSecurity, ILightDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IMailSender mailSender, IDataCache dataCache)
        {
            _userSecurity = userSecurity;
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _mailSender = mailSender;
            _dataCache = dataCache;
        }

        public void ValidateSecret(string secret)
        {
            var request = _userSecurity.DecryptObject<ApproveRequestModel>(secret);

            if (request == null)
            {
                Success = false;
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.Query().From<User>().Where("Id = $ AND IsBlocked = $", request.UserId, false).FirstOrDefault<User>();

                if (user == null)
                {
                    Success = false;
                    return;
                }

                if (user.IsApproved)
                {
                    Success = false;
                    return;
                }

                if (request.IsApprove)
                {
                    user.IsApproved = true;
                }
                else
                {
                    user.IsBlocked = true;
                }

                domainModel.Update(user);

                _changeTracker.AddChange(user, EntityChangeType.Updated, domainModel);

                if (user.IsApproved)
                {
                    _mailSender.SendMail(null, user.Id, null, Constants.MailTemplates.ConfirmUserApproval, new SuccessfulApprovalModel
                    {
                        SiteName = _dataCache.Get<ISiteConfiguration>().SiteName
                    });
                }
            }
        }
    }
}
