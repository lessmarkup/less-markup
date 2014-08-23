using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Security;
using LessMarkup.Engine.Configuration;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule.Model
{
    public class ValidateApprovalModel
    {
        private readonly IUserSecurity _userSecurity;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly IMailSender _mailSender;
        private readonly IDataCache _dataCache;

        public bool Success { get; set; }

        public ValidateApprovalModel(IUserSecurity userSecurity, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IMailSender mailSender, IDataCache dataCache)
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
                var user = domainModel.GetCollection<User>().FirstOrDefault(u => u.Id == request.UserId && !u.IsBlocked);

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

                _changeTracker.AddChange(user, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();

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
