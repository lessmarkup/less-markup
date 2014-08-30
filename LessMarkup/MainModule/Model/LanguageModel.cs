using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Serialization;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Language;
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
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = MainModuleTextIds.Language)]
    public class LanguageModel
    {
        public class Collection : IEditableModelCollection<LanguageModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IDataCache _dataCache;
            private readonly ICurrentUser _currentUser;
            private readonly IChangeTracker _changeTracker;

            public Collection(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _dataCache = dataCache;
                _currentUser = currentUser;
                _changeTracker = changeTracker;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return domainModel.GetSiteCollection<Language>().Select(l => l.Id);
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<Language>(); } }

            public IQueryable<LanguageModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetSiteCollection<Language>()
                    .Where(l => ids.Contains(l.Id))
                    .Select(l => new LanguageModel
                    {
                        Id = l.Id,
                        Name = l.Name,
                        ShortName = l.ShortName,
                        Visible = l.Visible,
                        IconId = l.IconId,
                        IsDefault = l.IsDefault,
                    });
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

                    domainModel.GetSiteCollection<Language>().Add(language);

                    domainModel.SaveChanges();
                    record.Id = language.Id;

                    domainModel.AutoDetectChangesEnabled = false;

                    try
                    {
                        foreach (var source in _dataCache.Get<ILanguageCache>().DefaultTranslations)
                        {
                            var translation = new Translation
                            {
                                LanguageId = record.Id,
                                Key = source.Key,
                                Text = source.Value
                            };

                            domainModel.GetSiteCollection<Translation>().Add(translation);
                        }
                    }
                    finally
                    {
                        domainModel.AutoDetectChangesEnabled = true;
                    }

                    _changeTracker.AddChange(language, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();
                }
            }

            public void UpdateRecord(LanguageModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var language = domainModel.GetSiteCollection<Language>().First(l => l.Id == record.Id);

                    language.Name = record.Name;
                    language.ShortName = record.ShortName;
                    language.Visible = record.Visible;

                    if (record.IconFile != null)
                    {
                        language.IconId = ImageUploader.SaveImage(domainModel, language.IconId, record.IconFile,
                            _currentUser.UserId, _dataCache.Get<ISiteConfiguration>());
                    }

                    _changeTracker.AddChange(language, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var language in domainModel.GetSiteCollection<Language>().Where(l => recordIds.Contains(l.Id)))
                    {
                        if (language.IconId.HasValue)
                        {
                            var image = domainModel.GetSiteCollection<Image>().First(i => i.Id == language.IconId);
                            domainModel.GetSiteCollection<Image>().Remove(image);
                        }
                        domainModel.GetSiteCollection<Language>().Remove(language);
                        _changeTracker.AddChange(language, EntityChangeType.Removed, domainModel);
                    }
                    domainModel.SaveChanges();
                }

                return true;
            }

            public bool DeleteOnly { get { return false; } }
        }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IDataCache _dataCache;
        private readonly IChangeTracker _changeTracker;

        public LanguageModel()
        {
        }

        public LanguageModel(IDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _changeTracker = changeTracker;
        }

        public void Reset()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                domainModel.AutoDetectChangesEnabled = false;
                try
                {
                    foreach (var translation in domainModel.GetSiteCollection<Translation>().Where(t => t.LanguageId == Id))
                    {
                        domainModel.GetSiteCollection<Translation>().Remove(translation);
                    }

                    foreach (var source in _dataCache.Get<ILanguageCache>().DefaultTranslations)
                    {
                        var translation = new Translation
                        {
                            LanguageId = Id,
                            Key = source.Key,
                            Text = source.Value
                        };

                        domainModel.GetSiteCollection<Translation>().Add(translation);
                    }
                }
                finally
                {
                    domainModel.AutoDetectChangesEnabled = true;
                }

                _changeTracker.AddChange<Language>(Id, EntityChangeType.Updated, domainModel);
                
                domainModel.SaveChanges();
            }
        }

        public void AddMissing()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var existing = new HashSet<string>(domainModel.GetSiteCollection<Translation>().Where(t => t.LanguageId == Id).Select(t => t.Key));

                try
                {
                    domainModel.AutoDetectChangesEnabled = false;

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

                        domainModel.GetSiteCollection<Translation>().Add(translation);
                    }
                }
                finally
                {
                    domainModel.AutoDetectChangesEnabled = true;
                }

                _changeTracker.AddChange<Language>(Id, EntityChangeType.Updated, domainModel);

                domainModel.SaveChanges();
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
                var language = domainModel.GetSiteCollection<Language>().First(l => l.Id == Id);

                language.Name = languageImport.Name;
                language.ShortName = languageImport.ShortName;

                var existingTranslations =
                    domainModel.GetSiteCollection<Translation>()
                        .Where(t => t.LanguageId == Id)
                        .ToDictionary(t => t.Key, t => t);

                domainModel.AutoDetectChangesEnabled = false;

                try
                {
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

                            domainModel.GetSiteCollection<Translation>().Add(translation);
                        }

                        translation.Text = source.Text;
                    }
                }
                finally
                {
                    domainModel.AutoDetectChangesEnabled = true;
                }

                _changeTracker.AddChange(language, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }

        public ActionResult Export()
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var language = domainModel.GetSiteCollection<Language>().First(l => l.Id == Id);

                var import = new LanguageImport
                {
                    Name = language.Name, 
                    ShortName = language.ShortName,
                    Translations = new List<TranslationImport>()
                };

                foreach (var translation in domainModel.GetSiteCollection<Translation>().Where(t => t.LanguageId == Id))
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
                foreach (var record in domainModel.GetSiteCollection<Language>().Where(l => l.IsDefault))
                {
                    record.IsDefault = false;
                    _changeTracker.AddChange(record, EntityChangeType.Updated, domainModel);
                }

                var language = domainModel.GetSiteCollection<Language>().First(l => l.Id == Id);
                language.IsDefault = true;
                _changeTracker.AddChange(language, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
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
