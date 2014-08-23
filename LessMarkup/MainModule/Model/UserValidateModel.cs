using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Security;
using LessMarkup.Engine.Configuration;
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
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IMailSender _mailSender;

        public bool Success { get; set; }
        public bool ApproveRequired { get; set; }

        public UserValidateModel(IUserSecurity userSecurity, IDataCache dataCache, IDomainModelProvider domainModelProvider, IMailSender mailSender)
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

                List<long> administrators;

                using (var domainModel = _domainModelProvider.Create())
                {
                    var user = domainModel.GetCollection<User>().First(u => u.Id == userId);

                    approveModel.UserName = user.Name;
                    approveModel.Email = user.Email;

                    administrators = domainModel.GetCollection<User>().Where(u => u.IsAdministrator && u.SiteId == user.SiteId && !u.IsRemoved && !u.IsBlocked).Select(u => u.Id).ToList();
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
