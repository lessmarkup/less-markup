/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.Smile, CollectionType = typeof(Collection))]
    public class SmileModel
    {
        public class Collection : IEditableModelCollection<SmileModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;

            public Collection(IDomainModelProvider domainModelProvider)
            {
                _domainModelProvider = domainModelProvider;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return RecordListHelper.GetFilterAndOrderQuery(domainModel.GetSiteCollection<Smile>(), filter, typeof (SmileModel)).Select(s => s.Id);
            }

            public int CollectionId { get { return DataHelper.GetCollectionIdVerified<Smile>(); } }

            public IQueryable<SmileModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<Smile>().Where(s => ids.Contains(s.Id)).Select(s => new SmileModel
                    {
                        Code = s.Code,
                        Name = s.Name,
                        Id = s.Id
                    });
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

                    domainModel.AddSiteObject(smile);
                    domainModel.SaveChanges();
                }
            }

            public void UpdateRecord(SmileModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var smile = domainModel.GetSiteCollection<Smile>().First(s => s.Id == record.Id);

                    smile.Code = record.Code;
                    smile.Name = record.Name;

                    if (record.Image != null)
                    {
                        smile.Data = record.Image.File;
                        smile.ContentType = record.Image.Type;
                    }

                    domainModel.SaveChanges();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var smile in domainModel.GetSiteCollection<Smile>().Where(s => recordIds.Contains(s.Id)))
                    {
                        domainModel.RemoveSiteObject(smile);
                    }

                    domainModel.SaveChanges();

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
