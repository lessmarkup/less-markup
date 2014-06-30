/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Security
{
    public interface IUserSecurity
    {
        void ChangePassword(string password, out string salt, out string encodedPassword);
        long CreateUser(string username, string password, string email, string address, Func<string, string> confirmation, bool generatePassword = false);
        string CreatePasswordValidationToken(long? userId);
        string CreateAccessToken(EntityType entityType, long entityId, EntityAccessType accessType, long? userId, DateTime? expirationTime = null);
        bool ValidateAccessToken(string token, EntityType entityType, long entityId, EntityAccessType accessType, long? userId);
        string GenerateUniqueId();
    }
}
