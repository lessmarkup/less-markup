﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.User;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(CollectionManager), TitleTextId = UserInterfaceTextIds.EditAdministrator, EntityType = EntityType.User)]
    public class GlobalAdministratorModel
    {
        public class CollectionManager : IEditableModelCollection<GlobalAdministratorModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IUserSecurity _userSecurity;
            private readonly IChangeTracker _changeTracker;

            public CollectionManager(IDomainModelProvider domainModelProvider, IUserSecurity userSecurity, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _userSecurity = userSecurity;
                _changeTracker = changeTracker;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetCollection<User>().Where(u => !u.SiteId.HasValue && !u.IsRemoved && !u.IsBlocked && u.IsAdministrator).Select(u => u.UserId);
            }

            public IQueryable<GlobalAdministratorModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetCollection<User>()
                        .Where(u => ids.Contains(u.UserId))
                        .Select(u => new GlobalAdministratorModel
                        {
                            UserId = u.UserId,
                            Email = u.Email,
                            Name = u.Name
                        });
            }

            public bool Filtered { get { return false; } }

            public GlobalAdministratorModel AddRecord(GlobalAdministratorModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var user = new User
                    {
                        Email = record.Email,
                        Name = record.Name,
                        Registered = DateTime.UtcNow,
                        IsBlocked = false,
                        IsValidated = true,
                        LastPasswordChanged = DateTime.UtcNow,
                        IsAdministrator = true
                    };

                    string userSalt, encodedPassword;
                    _userSecurity.ChangePassword(record.Password, out userSalt, out encodedPassword);

                    user.Password = encodedPassword;
                    user.Salt = userSalt;
                    user.PasswordAutoGenerated = false;

                    domainModel.GetCollection<User>().Add(user);
                    domainModel.SaveChanges();

                    _changeTracker.AddChange(user.UserId, EntityType.User, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();

                    domainModel.CompleteTransaction();

                    user.Password = null;

                    return new GlobalAdministratorModel
                    {
                        Email = user.Email,
                        Name = user.Name,
                        UserId = user.UserId
                    };
                }
            }

            public GlobalAdministratorModel UpdateRecord(GlobalAdministratorModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var user = domainModel.GetCollection<User>().First(u => u.IsAdministrator && !u.IsRemoved && u.UserId == record.UserId && !u.SiteId.HasValue);

                    user.Name = record.Name;
                    user.Email = record.Email;

                    if (!string.IsNullOrWhiteSpace(record.Password))
                    {
                        string userSalt, encodedPassword;
                        _userSecurity.ChangePassword(record.Password, out userSalt, out encodedPassword);

                        user.Password = encodedPassword;
                        user.Salt = userSalt;
                        user.PasswordAutoGenerated = false;
                        user.LastPasswordChanged = DateTime.UtcNow;
                    }

                    _changeTracker.AddChange(user.UserId, EntityType.User, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    return new GlobalAdministratorModel
                    {
                        Email = user.Email,
                        Name = user.Name,
                        UserId = user.UserId
                    };
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var userId in recordIds)
                    {
                        var user = domainModel.GetCollection<User>().First(u => u.IsAdministrator && !u.IsRemoved && u.UserId == userId && !u.SiteId.HasValue);
                        domainModel.GetCollection<User>().Remove(user);
                        _changeTracker.AddChange(userId, EntityType.User, EntityChangeType.Removed, domainModel);
                    }
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                }
                return true;
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long UserId { get; set; }

        [Column(UserInterfaceTextIds.UserName)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.UserName, Required = true)]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.UserEmail)]
        [InputField(InputFieldType.Email, UserInterfaceTextIds.UserEmail, Required = true)]
        public string Email { get; set; }

        [InputField(InputFieldType.Password, UserInterfaceTextIds.Password, Required = true)]
        public string Password { get; set; }
    }
}
