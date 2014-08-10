/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace LessMarkup.Engine.Security
{
    class CookieUserModel
    {
        public long UserId { get; set; }
        public string Email { get; set; }
        public IReadOnlyList<long> Groups { get; set; }
        public bool IsAdministrator { get; set; }
        public bool IsValidated { get; set; }
        public bool IsGlobalAdministrator { get; set; }
        public bool IsFakeUser { get; set; }
    }
}
