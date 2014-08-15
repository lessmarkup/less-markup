/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using LessMarkup.DataObjects.User;
using LessMarkup.Engine.Helpers;
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
        private readonly IDataCache _dataCache;

        public UserProfileModel(IDomainModelProvider domainModelProvider, ICurrentUser currentUser, IUserSecurity userSecurity, IChangeTracker changeTracker, IDataCache dataCache)
        {
            _currentUser = currentUser;
            _domainModelProvider = domainModelProvider;
            _userSecurity = userSecurity;
            _changeTracker = changeTracker;
            _dataCache = dataCache;
        }

        [InputField(InputFieldType.Text, MainModuleTextIds.Name)]
        public string Name { get; set; }

        [InputField(InputFieldType.PasswordRepeat, MainModuleTextIds.Password)]
        public string Password { get; set; }

        [InputField(InputFieldType.Image, MainModuleTextIds.ProfileAvatar)]
        public long? Avatar { get; set; }

        public InputFile AvatarFile { get; set; }

        public void Initialize()
        {
            Password = "";

            if (!_currentUser.UserId.HasValue)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.GetCollection<User>().Single(u => u.Id == _currentUser.UserId.Value);

                Name = user.Name;
                Avatar = user.AvatarImageId;
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
                var user = domainModel.GetCollection<User>().Single(u => u.Id == _currentUser.UserId.Value);

                user.Name = Name;

                if (AvatarFile != null)
                {
                    user.AvatarImageId = ImageUploader.SaveImage(domainModel, user.AvatarImageId, AvatarFile, _currentUser, _dataCache);
                }

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    string salt, encodedPassword;
                    _userSecurity.ChangePassword(Password, out salt, out encodedPassword);
                    user.Salt = salt;
                    user.Password = encodedPassword;
                }

                _changeTracker.AddChange<User>(user.Id, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }
        }
    }
}
