/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces.Exceptions;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Language
{
    public class CachedLanguage : ILanguage
    {
        public long LanguageId { get; set; }
        public string Name { get; set; }
        public long? IconId { get; set; }
        public string ShortName { get; set; }
        public bool IsDefault { get; set; }

        public class Translation
        {
            public string Reference { get; set; }
            public string Text { get; set; }
        }

        public List<Translation> Translations { get; set; }
        public Dictionary<string, string> TranslationsMap { get; set; }

        public string GetText(string id, bool throwIfNotFound = true)
        {
            string cachedTranslation;
            if (TranslationsMap.TryGetValue(id, out cachedTranslation))
            {
                return cachedTranslation;
            }

            if (throwIfNotFound)
            {
                throw new TextNotFoundException(id);
            }

            return null;
        }
    }
}
