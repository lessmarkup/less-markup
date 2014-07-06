/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Text.RegularExpressions;

namespace LessMarkup.Engine.Security
{
    public static class EmailCheck
    {
        private const string RegexPattern = @"^[a-z][a-z|0-9|]*([_][a-z|0-9]+)*([.][a-z|0-9]+([_][a-z|0-9]+)*)?@[a-z][a-z|0-9|]*\.([a-z][a-z|0-9]*(\.[a-z][a-z|0-9]*)?)$";

        public static bool IsValidEmail(string email)
        {
            if (email != null)
            {
                email = email.Trim();
            }

            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            if (email.Length > 100)
            {
                return false;
            }

            var match = Regex.Match(email.Trim(), RegexPattern, RegexOptions.IgnoreCase);

            return match.Success;
        }
    }
}
