/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework;
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
            private readonly ILightDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public Collection(ILightDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.From<Translation>().Where("LanguageId = $", _languageId).ToIdList();
            }

            public int CollectionId { get; private set; }

            public IReadOnlyCollection<TranslationModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<Translation>().WhereIds(ids).ToList<TranslationModel>();
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

                    domainModel.Create(translation);
                    _changeTracker.AddChange<Language>(_languageId, EntityChangeType.Updated, domainModel);
                }
            }

            public void UpdateRecord(TranslationModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var translation = domainModel.Query().From<Translation>().Where("LanguageId = $ AND Id = $", _languageId, record.Id).First<Translation>();
                    translation.Key = record.Key;
                    translation.Text = record.Text;
                    domainModel.Update(translation);
                    _changeTracker.AddChange<Language>(_languageId, EntityChangeType.Updated, domainModel);
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var recordId in recordIds)
                    {
                        domainModel.Delete<Translation>(recordId);
                    }

                    _changeTracker.AddChange<Language>(_languageId, EntityChangeType.Updated, domainModel);
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
