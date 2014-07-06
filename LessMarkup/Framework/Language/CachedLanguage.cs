/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces.Exceptions;

namespace LessMarkup.Engine.Language
{
    public class CachedLanguage
    {
        public long LanguageId { get; set; }
        public string Name { get; set; }
        public long? IconId { get; set; }
        public string ShortName { get; set; }
        public bool IsDefault { get; set; }

        private readonly Dictionary<string, CachedTranslation> _translations = new Dictionary<string, CachedTranslation>();

        public void AddTranslation(string key, CachedTranslation value)
        {
            _translations[key] = value;
        }

        public string GetText(string id, bool throwIfNotFound = true)
        {
            CachedTranslation cachedTranslation;
            if (_translations.TryGetValue(id, out cachedTranslation))
            {
                return cachedTranslation.Text;
            }

            if (throwIfNotFound)
            {
                throw new TextNotFoundException(id);
            }

            return null;
        }
    }
}
