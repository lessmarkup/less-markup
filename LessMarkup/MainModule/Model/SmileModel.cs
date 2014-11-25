/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.Smile, CollectionType = typeof(Collection), DataType = typeof(Smile))]
    public class SmileModel
    {
        public class Collection : IEditableModelCollection<SmileModel>
        {
            private readonly ILightDomainModelProvider _domainModelProvider;

            public Collection(ILightDomainModelProvider domainModelProvider)
            {
                _domainModelProvider = domainModelProvider;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.From<Smile>().ToIdList();
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<Smile>(); } }

            public IReadOnlyCollection<SmileModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<Smile>().WhereIds(ids).ToList<SmileModel>();
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public SmileModel CreateRecord()
            {
                return new SmileModel();
            }

            public void AddRecord(SmileModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var smile = new Smile
                    {
                        Name = record.Name,
                        Code = record.Code,
                        ContentType = record.Image.Type,
                        Data = record.Image.File,
                    };

                    domainModel.Create(smile);
                }
            }

            public void UpdateRecord(SmileModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var smile = domainModel.Query().Find<Smile>(record.Id);

                    smile.Code = record.Code;
                    smile.Name = record.Name;

                    if (record.Image != null)
                    {
                        smile.Data = record.Image.File;
                        smile.ContentType = record.Image.Type;
                    }

                    domainModel.Update(smile);
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var id in recordIds)
                    {
                        domainModel.Delete<Smile>(id);
                    }

                    return true;
                }
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long Id { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Text, Required = true)]
        [Column(MainModuleTextIds.Text)]
        [RecordSearch]
        public string Name { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Code, Required = true)]
        [Column(MainModuleTextIds.Code)]
        [RecordSearch]
        public string Code { get; set; }

        [InputField(InputFieldType.File, MainModuleTextIds.Image, Required = true)]
        public InputFile Image { get; set; }
    }
}
