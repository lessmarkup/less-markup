/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using LessMarkup.DataObjects.User;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.MainModule.Model
{
    [RecordModel]
    public class UserProfileModel
    {
        private readonly ICurrentUser _currentUser;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IUserSecurity _userSecurity;
        private readonly IChangeTracker _changeTracker;

        public UserProfileModel(IDomainModelProvider domainModelProvider, ICurrentUser currentUser, IUserSecurity userSecurity, IChangeTracker changeTracker)
        {
            _currentUser = currentUser;
            _domainModelProvider = domainModelProvider;
            _userSecurity = userSecurity;
            _changeTracker = changeTracker;
        }

        [InputField(InputFieldType.Text, MainModuleTextIds.Name)]
        public string Name { get; set; }

        [InputField(InputFieldType.Password, MainModuleTextIds.Password)]
        public string Password { get; set; }

        public void Initialize()
        {
            Password = "";

            if (!_currentUser.UserId.HasValue)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.GetCollection<User>().Single(u => u.UserId == _currentUser.UserId.Value);

                Name = user.Name;
            }
        }

        public void Save()
        {
            if (!_currentUser.UserId.HasValue)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.CreateWithTransaction())
            {
                var user = domainModel.GetCollection<User>().Single(u => u.UserId == _currentUser.UserId.Value);

                user.Name = Name;

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    string salt, encodedPassword;
                    _userSecurity.ChangePassword(Password, out salt, out encodedPassword);
                    user.Salt = salt;
                    user.Password = encodedPassword;
                }

                _changeTracker.AddChange(user.UserId, EntityType.User, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }
        }
    }
}
