/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Helpers
{
    public static class LanguageHelper
    {
        private static ILanguageCache GetLanguageCache()
        {
            var dataCache = DependencyResolver.Resolve<IDataCache>();
            var languageCache = dataCache.Get<ILanguageCache>();
            return languageCache;
        }

        public static string GetText(string moduleType, object id, params object[] args)
        {
            if (id == null)
            {
                return null;
            }

            var translation = GetLanguageCache().GetTranslation(id.ToString(), moduleType);

            if (args == null || args.Length == 0)
            {
                return translation;
            }

            return string.Format(translation, args);
        }

        public static string GetTextWithDefault(string moduleType, object id, object defaultId, string defaultModuleType, params object[] args)
        {
            if (id == null)
            {
                return null;
            }

            var languageCache = GetLanguageCache();

            var text = languageCache.GetTranslation(id.ToString(), moduleType, false);

            if (text == null && defaultId != null)
            {
                text = languageCache.GetTranslation(defaultId.ToString(), defaultModuleType);
            }

            return text == null ? null : string.Format(text, args);
        }
    }
}
