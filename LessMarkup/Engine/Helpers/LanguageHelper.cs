/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Engine.Language;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Engine.Helpers
{
    public static class LanguageHelper
    {
        private static LanguageCache GetLanguageCache()
        {
            var dataCache = DependencyResolver.Resolve<IDataCache>();
            var languageCache = dataCache.Get<LanguageCache>();
            return languageCache;
        }

        public static string GetText(string moduleType, object id, params object[] args)
        {
            if (id == null)
            {
                return null;
            }
            return string.Format(GetLanguageCache().GetTranslation(id.ToString(), moduleType), args);
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
