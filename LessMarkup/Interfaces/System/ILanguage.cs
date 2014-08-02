/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.System
{
    public interface ILanguage
    {
        long LanguageId { get; set; }
        string Name { get; set; }
        long? IconId { get; set; }
        string ShortName { get; set; }
        bool IsDefault { get; set; }
    }
}
