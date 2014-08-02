/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using AutoMapper;
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
    public class LanguageCache : ILanguageCache
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleProvider _moduleProvider;
        private readonly Dictionary<long, CachedLanguage> _languagesMap = new Dictionary<long, CachedLanguage>();
        private const string CookieLanguage = "lang";
        private long? _defaultLanguageId;
        private List<CachedLanguage> _languagesList;

        private readonly Dictionary<string, string> _defaultTranslations = new Dictionary<string, string>();

        public LanguageCache(IDomainModelProvider domainModelProvider, IModuleProvider moduleProvider)
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

        public void Initialize(long? siteId, out DateTime? expirationTime, long? objectId = null)
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
                foreach (var language in domainModel.GetCollection<DataObjects.Common.Language>().Where(l => l.Visible).Include(l => l.Translations))
                {
                    var cachedLanguage = new CachedLanguage
                    {
                        Name = language.Name,
                        IconId = language.IconId,
                        IsDefault = language.IsDefault,
                        LanguageId = language.LanguageId,
                        ShortName = language.ShortName
                    };

                    foreach (var translation in language.Translations)
                    {
                        var cachedTranslation = Mapper.DynamicMap<CachedTranslation>(translation);
                        cachedLanguage.AddTranslation(translation.Reference, cachedTranslation);
                    }

                    _languagesMap[language.LanguageId] = cachedLanguage;
                }
            }

            var defaultLanguage = _languagesMap.Values.FirstOrDefault(l => l.IsDefault) ?? _languagesMap.Values.FirstOrDefault();
            if (defaultLanguage != null)
            {
                _defaultLanguageId = defaultLanguage.LanguageId;
            }

            _languagesList = _languagesMap.Values.ToList();

            expirationTime = null;
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

        public List<ILanguage> Languages
        {
            get { return _languagesList.Select(l => (ILanguage) l).ToList(); }
        }

        public string GetTranslation(string id, string moduleType, bool throwIfNotFound = true)
        {
            var language = CurrentLanguage;

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

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.Language;
        }

        public EntityType[] HandledTypes { get { return new[] {EntityType.Language}; } }
    }
}
