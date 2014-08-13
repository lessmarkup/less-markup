/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.EditFile, CollectionType = typeof(Collection))]
    public class FileModel
    {
        private readonly IDomainModelProvider _domainModelProvider;

        public FileModel(IDomainModelProvider domainModelProvider)
        {
            _domainModelProvider = domainModelProvider;
        }

        public FileModel()
        {
        }

        public class Collection : IEditableModelCollection<FileModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;

            public Collection(IDomainModelProvider domainModelProvider)
            {
                _domainModelProvider = domainModelProvider;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetSiteCollection<File>().Select(f => f.Id);
            }

            public int CollectionId { get { return AbstractDomainModel.GetCollectionId<File>(); } }

            public IQueryable<FileModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<File>()
                        .Where(f => ids.Contains(f.Id))
                        .Select(f => new FileModel { FileId = f.Id, UniqueId = f.UniqueId, FileName = f.FileName });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public void AddRecord(FileModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var file = new File
                    {
                        Data = record.File.File,
                        ContentType = record.File.Type,
                        UniqueId = record.UniqueId,
                        FileName = record.FileName,
                    };

                    if (string.IsNullOrWhiteSpace(file.FileName))
                    {
                        file.FileName = record.File.Name;
                    }

                    domainModel.GetSiteCollection<File>().Add(file);

                    domainModel.SaveChanges();

                    record.File = new InputFile();
                    record.FileName = file.FileName;
                    record.FileId = file.Id;
                }
            }

            public void UpdateRecord(FileModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var file = domainModel.GetSiteCollection<File>().Single(f => f.Id == record.FileId);

                    file.UniqueId = record.UniqueId;
                    file.FileName = record.FileName;

                    if (record.File != null)
                    {
                        file.Data = record.File.File;
                        file.ContentType = record.File.Type;

                        if (string.IsNullOrWhiteSpace(file.FileName))
                        {
                            file.FileName = record.File.Name;
                        }
                    }

                    domainModel.SaveChanges();

                    record.File = new InputFile();
                    record.FileName = file.FileName;
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var file in domainModel.GetSiteCollection<File>().Where(f => recordIds.Contains(f.Id)))
                    {
                        domainModel.GetSiteCollection<File>().Remove(file);
                    }
                    domainModel.SaveChanges();
                }
                return true;
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long FileId { get; set; }
        [Column(MainModuleTextIds.UniqueId)]
        [InputField(InputFieldType.Text, MainModuleTextIds.UniqueId, Required = true)]
        public string UniqueId { get; set; }
        [Column(MainModuleTextIds.FileName)]
        [InputField(InputFieldType.Text, MainModuleTextIds.FileName)]
        public string FileName { get; set; }
        [InputField(InputFieldType.File, MainModuleTextIds.File, Required = true)]
        public InputFile File { get; set; }

        public ActionResult GetFile(string id)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var file = domainModel.GetSiteCollection<File>().FirstOrDefault(f => f.UniqueId == id);

                if (file == null)
                {
                    return new HttpNotFoundResult();
                }

                return new FileContentResult(file.Data, file.ContentType) {FileDownloadName = file.FileName};
            }
        }
    }
}
