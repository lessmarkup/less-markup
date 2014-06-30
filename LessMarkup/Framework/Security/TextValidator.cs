/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;

namespace LessMarkup.Framework.Security
{
    public class TextValidator
    {
        private const int MaximumUsernameLength = 30;
        private const int MaximumPasswordLength = 100;
        private const int MaximumTextLength = 128;
        private const int MinimumPasswordLetterOrDigit = 4;
        private const int MinimumUsernameLength = 6;
        private const int MinimumPasswordLength = 7;

        public static bool CheckPassword(string text)
        {
            return CheckString(text, MinimumPasswordLength, MaximumPasswordLength);
        }

        public static bool CheckNewPassword(string text)
        {
            if (!CheckPassword(text))
            {
                return false;
            }

            if (!text.Any(Char.IsDigit))
            {
                return false;
            }

            if (!text.Any(Char.IsUpper))
            {
                return false;
            }

            if (!text.Any(Char.IsLower))
            {
                return false;
            }

            if (text.Count(Char.IsLetterOrDigit) < MinimumPasswordLetterOrDigit)
            {
                return false;
            }

            return true;
        }

        public static bool CheckUsername(string text)
        {
            return CheckString(text, MinimumUsernameLength, MaximumUsernameLength);
        }

        public static bool CheckTextField(string text)
        {
            return CheckString(text, 0, MaximumTextLength);
        }

        private static bool CheckString(string text, int minLength, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return minLength == 0;
            }

            if (text.Length > maxLength)
            {
                return false;
            }

            if (text.Any(x => Char.IsControl(x) || Char.IsSeparator(x) || Char.IsWhiteSpace(x)))
            {
                return false;
            }

            return true;
        }
    }
}
