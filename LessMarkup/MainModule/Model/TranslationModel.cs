/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.Translation, CollectionType = typeof(Collection))]
    public class TranslationModel
    {
        public class Collection : IEditableModelCollection<TranslationModel>
        {
            private long _languageId;
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public Collection(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return RecordListHelper.GetFilterAndOrderQuery(domainModel.GetSiteCollection<Translation>().Where(t => t.LanguageId == _languageId), filter, typeof (TranslationModel)).Select(t => t.Id);
            }

            public int CollectionId { get; private set; }
            public IQueryable<TranslationModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<Translation>()
                        .Where(t => ids.Contains(t.Id))
                        .Select(t => new TranslationModel
                        {
                            Id = t.Id,
                            Key = t.Key,
                            Text = t.Text
                        });
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
                if (!objectId.HasValue)
                {
                    throw new ArgumentNullException("objectId");
                }

                _languageId = objectId.Value;
            }

            public TranslationModel CreateRecord()
            {
                return new TranslationModel();
            }

            public void AddRecord(TranslationModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var translation = new Translation
                    {
                        Key = record.Key,
                        Text = record.Text,
                        LanguageId = _languageId
                    };

                    domainModel.GetSiteCollection<Translation>().Add(translation);
                    _changeTracker.AddChange<Language>(_languageId, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                }
            }

            public void UpdateRecord(TranslationModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var translation = domainModel.GetSiteCollection<Translation>().First(t => t.Id == record.Id && t.LanguageId == _languageId);
                    translation.Key = record.Key;
                    translation.Text = record.Text;
                    _changeTracker.AddChange<Language>(_languageId, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var translation in domainModel.GetSiteCollection<Translation>().Where(t => recordIds.Contains(t.Id)))
                    {
                        domainModel.GetSiteCollection<Translation>().Remove(translation);
                    }
                    _changeTracker.AddChange<Language>(_languageId, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    return true;
                }
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long Id { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Key, Required = true)]
        [Column(MainModuleTextIds.Key)]
        [RecordSearch]
        public string Key { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Text)]
        [Column(MainModuleTextIds.Text)]
        [RecordSearch]
        public string Text { get; set; }
    }
}
