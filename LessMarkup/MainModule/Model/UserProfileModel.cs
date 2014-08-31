/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Security;
using LessMarkup.Engine.Language;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

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
        private readonly IHtmlSanitizer _htmlSanitizer;

        public UserProfileModel(IDomainModelProvider domainModelProvider, ICurrentUser currentUser, IUserSecurity userSecurity, IChangeTracker changeTracker, IDataCache dataCache, IHtmlSanitizer htmlSanitizer)
        {
            _currentUser = currentUser;
            _domainModelProvider = domainModelProvider;
            _userSecurity = userSecurity;
            _changeTracker = changeTracker;
            _dataCache = dataCache;
            _htmlSanitizer = htmlSanitizer;
        }

        [InputField(InputFieldType.Text, MainModuleTextIds.Name)]
        public string Name { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Title)]
        public string Title { get; set; }

        [InputField(InputFieldType.PasswordRepeat, MainModuleTextIds.Password)]
        public string Password { get; set; }

        [InputField(InputFieldType.Image, MainModuleTextIds.ProfileAvatar)]
        public long? Avatar { get; set; }

        [InputField(InputFieldType.RichText, MainModuleTextIds.Signature)]
        public string Signature { get; set; }

        public InputFile AvatarFile { get; set; }

        [InputField(InputFieldType.DynamicFieldList)]
        public List<DynamicInputProperty> Properties { get; set; }

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
                Signature = user.Signature;
                Title = user.Title;

                Dictionary<string, object> properties = null;
                if (!string.IsNullOrEmpty(user.Properties))
                {
                    properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(user.Properties);
                }

                foreach (var propertyDefinition in domainModel.GetSiteCollection<UserPropertyDefinition>())
                {
                    var property = new DynamicInputProperty
                    {
                        Field = new InputFieldModel()
                    };

                    property.Field.Text = propertyDefinition.Title;
                    property.Field.Property = propertyDefinition.Name;
                    switch (propertyDefinition.Type)
                    {
                        case UserPropertyType.Date:
                            property.Field.Type = InputFieldType.Date;
                            break;
                        case UserPropertyType.File:
                            property.Field.Type = InputFieldType.File;
                            break;
                        case UserPropertyType.Image:
                            property.Field.Type = InputFieldType.Image;
                            break;
                        case UserPropertyType.Note:
                            property.Field.Type = InputFieldType.MultiLineText;
                            break;
                        case UserPropertyType.Text:
                            property.Field.Type = InputFieldType.Text;
                            break;
                    }
                    if (properties != null && property.Field.Type != InputFieldType.File && property.Field.Type != InputFieldType.Image)
                    {
                        object propertyValue;
                        if (properties.TryGetValue(propertyDefinition.Name, out propertyValue))
                        {
                            property.Value = propertyValue;
                        }
                    }

                    if (Properties == null)
                    {
                        Properties = new List<DynamicInputProperty>();
                    }

                    Properties.Add(property);
                }
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
                user.Signature = _htmlSanitizer.Sanitize(Signature);
                user.Title = Title;

                if (AvatarFile != null)
                {
                    user.AvatarImageId = ImageUploader.SaveImage(domainModel, user.AvatarImageId, AvatarFile, _currentUser.UserId, _dataCache.Get<ISiteConfiguration>());
                }

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    string salt, encodedPassword;
                    _userSecurity.ChangePassword(Password, out salt, out encodedPassword);
                    user.Salt = salt;
                    user.Password = encodedPassword;
                }


                if (Properties != null)
                {
                    Dictionary<string, object> properties = null;
                    if (!string.IsNullOrEmpty(user.Properties))
                    {
                        properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(user.Properties);
                    }

                    foreach (var propertyDefinition in domainModel.GetSiteCollection<UserPropertyDefinition>())
                    {
                        var property = Properties.FirstOrDefault(p => p.Field != null && p.Field.Property == propertyDefinition.Name);

                        if (property == null || property.Value == null)
                        {
                            continue;
                        }

                        if (properties == null)
                        {
                            properties = new Dictionary<string, object>();
                        }

                        switch (propertyDefinition.Type)
                        {
                            case UserPropertyType.File:
                            case UserPropertyType.Image:
                                var inputFile = (InputFile) property.Value;
                                if (inputFile == null)
                                {
                                    continue;
                                }
                                break;
                            case UserPropertyType.Date:
                                if (property.Value.GetType() != typeof (DateTime))
                                {
                                    continue;
                                }
                                break;
                            case UserPropertyType.Note:
                            case UserPropertyType.Text:
                                if (property.Value.GetType() != typeof (string))
                                {
                                    continue;
                                }
                                break;
                        }
                        properties[propertyDefinition.Name] = property.Value;
                    }

                    if (properties != null)
                    {
                        user.Properties = JsonConvert.SerializeObject(properties);
                    }
                }

                _changeTracker.AddChange<User>(user.Id, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
                domainModel.CompleteTransaction();
            }
        }
    }
}
