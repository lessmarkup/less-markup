/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web.Mvc;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.Interfaces.Security
{
    public interface IUserSecurity
    {
        void ChangePassword(string password, out string salt, out string encodedPassword);
        long CreateUser(string username, string password, string email, string address, UrlHelper urlHelper, bool preApproved, bool generatePassword);
        string CreatePasswordChangeToken(long? userId);
        long? ValidatePasswordChangeToken(string token);
        string CreateAccessToken(int collectionId, long entityId, EntityAccessType accessType, long? userId, DateTime? expirationTime = null);
        bool ValidateAccessToken(string token, int collectionId, long entityId, EntityAccessType accessType, long? userId);
        string GenerateUniqueId();
        bool ConfirmUser(string validateSecret, out long userId);
        string EncryptObject(object obj);
        T DecryptObject<T>(string encrypted) where T : class;
        void ValidateInputFile(InputFile file);
    }
}
