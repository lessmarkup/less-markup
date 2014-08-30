/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
#if !DEBUG
using LessMarkup.Interfaces.Exceptions;
#endif
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Language
{
    public class LanguageCache : AbstractCacheHandler, ILanguageCache
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleProvider _moduleProvider;
        private Dictionary<long, CachedLanguage> _languagesMap;
        private const string CookieLanguage = "lang";
        private long? _defaultLanguageId;
        private List<CachedLanguage> _languagesList;

        private readonly Dictionary<string, string> _defaultTranslations = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> DefaultTranslations { get { return _defaultTranslations; } }

        public LanguageCache(IDomainModelProvider domainModelProvider, IModuleProvider moduleProvider)
            : base(new[] { typeof(DataObjects.Common.Language) })
        {
            _domainModelProvider = domainModelProvider;
            _moduleProvider = moduleProvider;
        }

        private void LoadTranslation(Assembly assembly, string moduleType)
        {
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("Language.xml"));

            if (string.IsNullOrEmpty(resourceName))
            {
                return;
            }

            using (var resource = assembly.GetManifestResourceStream(resourceName))
            {
                if (resource != null)
                {
                    using (var reader = new XmlTextReader(resource))
                    {
                        var languageFile = (XmlLanguageFile)new XmlSerializer(typeof(XmlLanguageFile)).Deserialize(reader);
                        if (languageFile.Translations != null)
                        {
                            foreach (var translation in languageFile.Translations)
                            {
                                if (!string.IsNullOrWhiteSpace(translation.Id))
                                {
                                    var key = moduleType + "." + translation.Id;
                                    if (_defaultTranslations.ContainsKey(key))
                                    {
                                        this.LogWarning(string.Format("Translation key '{0}' already exists", key));
                                    }
                                    _defaultTranslations[key] = translation.Text;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            if (objectId != null)
            {
                throw new ArgumentException("objectId");
            }

            foreach (var module in _moduleProvider.Modules)
            {
                LoadTranslation(module.Assembly, module.ModuleType);
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                _languagesMap = domainModel.GetSiteCollection<DataObjects.Common.Language>().Where(l => l.Visible)
                    .Select(l => new CachedLanguage
                    {
                        Name = l.Name,
                        IsDefault = l.IsDefault,
                        IconId = l.IconId,
                        LanguageId = l.Id,
                        ShortName = l.ShortName,
                        Translations = l.Translations.Select(t => new CachedLanguage.Translation { Reference = t.Key, Text = t.Text}).ToList()
                    })
                    .ToDictionary(l => l.LanguageId, l => l);

                foreach (var language in _languagesMap.Values)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    language.TranslationsMap = language.Translations.ToDictionary(t => t.Reference, t => t.Text);
                    language.Translations = null;
                }
            }

            var defaultLanguage = _languagesMap.Values.FirstOrDefault(l => l.IsDefault) ?? _languagesMap.Values.FirstOrDefault();
            if (defaultLanguage != null)
            {
                _defaultLanguageId = defaultLanguage.LanguageId;
            }

            _languagesList = _languagesMap.Values.ToList();
        }

        public long? CurrentLanguageId
        {
            get
            {
                var context = HttpContext.Current;

                var cookieLanguage = context.Request.Cookies[CookieLanguage];
                if (cookieLanguage != null)
                {
                    long languageId;
                    CachedLanguage language;
                    if (long.TryParse(cookieLanguage.Value, out languageId) && _languagesMap.TryGetValue(languageId, out language))
                    {
                        return languageId;
                    }
                }

                return _defaultLanguageId;
            }
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException();
                }
                var context = HttpContext.Current;
                context.Response.Cookies.Remove(CookieLanguage);
                context.Response.Cookies.Add(new HttpCookie(CookieLanguage, value.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private CachedLanguage CurrentLanguage
        {
            get
            {
                var currentLanguageId = CurrentLanguageId;
                if (!currentLanguageId.HasValue)
                {
                    return null;
                }
                return _languagesMap[currentLanguageId.Value];
            }
        }

        public string GetTranslation(long? languageId, string id, string moduleType, bool throwIfNotFound = true)
        {
            var language = languageId.HasValue ? _languagesMap[languageId.Value] : null;
            return GetTranslation(language, id, moduleType, throwIfNotFound);
        }

        public List<ILanguage> Languages
        {
            get { return _languagesList.Select(l => (ILanguage) l).ToList(); }
        }

        public string GetTranslation(string id, string moduleType, bool throwIfNotFound = true)
        {
            var language = CurrentLanguage;
            return GetTranslation(language, id, moduleType, throwIfNotFound);
        }

        private string GetTranslation(CachedLanguage language, string id, string moduleType, bool throwIfNotFound)
        {
            string translation;

            id = moduleType + "." + id;

            if (language != null)
            {
                translation = language.GetText(id, false);
                if (translation != null)
                {
                    return translation;
                }
            }

            if (_defaultTranslations.TryGetValue(id, out translation))
            {
                return translation;
            }

            if (!throwIfNotFound)
            {
                return null;
            }

#if DEBUG
            return "$$-"+id;
#else
            throw new TextNotFoundException(id);
#endif
        }
    }
}
