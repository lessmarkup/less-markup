/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Serialization;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.MainModule.Model.Xml;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = MainModuleTextIds.Language, DataType = typeof(Language))]
    public class LanguageModel
    {
        public class Collection : IEditableModelCollection<LanguageModel>
        {
            private readonly ILightDomainModelProvider _domainModelProvider;
            private readonly IDataCache _dataCache;
            private readonly ICurrentUser _currentUser;
            private readonly IChangeTracker _changeTracker;

            public Collection(ILightDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _dataCache = dataCache;
                _currentUser = currentUser;
                _changeTracker = changeTracker;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.From<Language>().ToIdList();
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<Language>(); } }

            public IReadOnlyCollection<LanguageModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<Language>().ToList<LanguageModel>();
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public LanguageModel CreateRecord()
            {
                return new LanguageModel();
            }

            public void AddRecord(LanguageModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var language = new Language
                    {
                        Name = record.Name,
                        ShortName = record.ShortName,
                        Visible = record.Visible,
                    };

                    if (record.IconFile != null)
                    {
                        language.IconId = ImageUploader.SaveImage(domainModel, null, record.IconFile,
                            _currentUser.UserId, _dataCache.Get<ISiteConfiguration>());
                    }

                    domainModel.Create(language);

                    record.Id = language.Id;

                    foreach (var source in _dataCache.Get<ILanguageCache>().DefaultTranslations)
                    {
                        var translation = new Translation
                        {
                            LanguageId = record.Id,
                            Key = source.Key,
                            Text = source.Value
                        };

                        domainModel.Create(translation);
                    }

                    _changeTracker.AddChange(language, EntityChangeType.Added, domainModel);
                }
            }

            public void UpdateRecord(LanguageModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var language = domainModel.Query().Find<Language>(record.Id);

                    language.Name = record.Name;
                    language.ShortName = record.ShortName;
                    language.Visible = record.Visible;

                    if (record.IconFile != null)
                    {
                        language.IconId = ImageUploader.SaveImage(domainModel, language.IconId, record.IconFile,
                            _currentUser.UserId, _dataCache.Get<ISiteConfiguration>());
                    }

                    domainModel.Update(language);
                    _changeTracker.AddChange(language, EntityChangeType.Updated, domainModel);
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var language in domainModel.Query().From<Language>().WhereIds(recordIds).ToList<Language>("Id, IconId"))
                    {
                        if (language.IconId.HasValue)
                        {
                            domainModel.Delete<Image>(language.IconId.Value);
                        }
                        domainModel.Delete<Language>(language.Id);
                        _changeTracker.AddChange(language, EntityChangeType.Removed, domainModel);
                    }
                }

                return true;
            }

            public bool DeleteOnly { get { return false; } }
        }

        private readonly ILightDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly IChangeTracker _changeTracker;

        public LanguageModel()
        {
        }

        public LanguageModel(ILightDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _changeTracker = changeTracker;
        }

        public void Reset()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var translation in domainModel.Query().From<Translation>().Where("LanguageId = $", Id).ToIdList())
                {
                    domainModel.Delete<Translation>(translation);
                }

                foreach (var source in _dataCache.Get<ILanguageCache>().DefaultTranslations)
                {
                    var translation = new Translation
                    {
                        LanguageId = Id,
                        Key = source.Key,
                        Text = source.Value
                    };

                    domainModel.Create(translation);
                }

                _changeTracker.AddChange<Language>(Id, EntityChangeType.Updated, domainModel);
            }
        }

        public void AddMissing()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var existing = new HashSet<string>(domainModel.Query().From<Translation>().Where("LanguageId = $", Id).ToList<Translation>("Key").Select(t => t.Key));

                foreach (var source in _dataCache.Get<ILanguageCache>().DefaultTranslations)
                {
                    if (existing.Contains(source.Key))
                    {
                        continue;
                    }

                    var translation = new Translation
                    {
                        LanguageId = Id,
                        Key = source.Key,
                        Text = source.Value
                    };

                    domainModel.Create(translation);
                }

                _changeTracker.AddChange<Language>(Id, EntityChangeType.Updated, domainModel);
            }
        }

        public void Import(InputFile file)
        {
            LanguageImport languageImport;

            using (var stream = new MemoryStream(file.File))
            {
                languageImport = (LanguageImport) new XmlSerializer(typeof (LanguageImport)).Deserialize(stream);
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var language = domainModel.Query().Find<Language>(Id);

                language.Name = languageImport.Name;
                language.ShortName = languageImport.ShortName;

                var existingTranslations = domainModel.Query().From<Translation>().Where("LanguageId = $", Id).ToList<Translation>().Where(t => t.LanguageId == Id).ToDictionary(t => t.Key, t => t);

                foreach (var source in languageImport.Translations)
                {
                    Translation translation;

                    if (!existingTranslations.TryGetValue(source.Key, out translation))
                    {
                        translation = new Translation
                        {
                            Key = source.Key,
                            LanguageId = Id,
                        };

                        domainModel.Create(translation);
                    }

                    translation.Text = source.Text;
                    domainModel.Update(translation);
                }

                _changeTracker.AddChange(language, EntityChangeType.Updated, domainModel);
            }
        }

        public ActionResult Export()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var language = domainModel.Query().Find<Language>(Id);

                var import = new LanguageImport
                {
                    Name = language.Name, 
                    ShortName = language.ShortName,
                    Translations = new List<TranslationImport>()
                };

                foreach (var translation in domainModel.Query().From<Translation>().Where("LanguageId = $", Id).ToList<Translation>())
                {
                    import.Translations.Add(new TranslationImport
                    {
                        Key = translation.Key,
                        Text = translation.Text
                    });
                }

                using (var stream = new MemoryStream())
                {
                    new XmlSerializer(typeof (LanguageImport)).Serialize(stream, import);
                    return new FileContentResult(stream.ToArray(), "text/xml") { FileDownloadName = "Export.xml" };
                }
            }
        }

        public void SetDefault()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var record in domainModel.Query().From<Language>().Where("IsDefault = $ AND Id != $", true, Id).ToList<Language>())
                {
                    record.IsDefault = false;
                    _changeTracker.AddChange(record, EntityChangeType.Updated, domainModel);
                    domainModel.Update(record);
                }

                var language = domainModel.Query().Find<Language>(Id);
                if (!language.IsDefault)
                {
                    language.IsDefault = true;
                    domainModel.Update(language);
                    _changeTracker.AddChange(language, EntityChangeType.Updated, domainModel);
                }
            }
        }

        public long Id { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Name)]
        [Column(MainModuleTextIds.Name)]
        public string Name { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.ShortName)]
        [Column(MainModuleTextIds.ShortName)]
        public string ShortName { get; set; }

        [InputField(InputFieldType.CheckBox, MainModuleTextIds.Visible)]
        [Column(MainModuleTextIds.Visible)]
        public bool Visible { get; set; }

        [Column(MainModuleTextIds.IsDefault)]
        public bool IsDefault { get; set; }

        public long? IconId { get; set; }

        [InputField(InputFieldType.Image, MainModuleTextIds.Icon)]
        public InputFile IconFile { get; set; }
    }
}
