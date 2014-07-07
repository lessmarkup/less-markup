using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.UserInterface;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.EditFile, CollectionType = typeof(Collection))]
    public class FileModel
    {
        public class Collection : IEditableModelCollection<FileModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;

            public Collection(IDomainModelProvider domainModelProvider)
            {
                _domainModelProvider = domainModelProvider;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetSiteCollection<File>().Select(f => f.FileId);
            }

            public IQueryable<FileModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<File>()
                        .Where(f => ids.Contains(f.FileId))
                        .Select(f => new FileModel { FileId = f.FileId, UniqueId = f.UniqueId });
            }

            public bool Filtered { get { return false; } }

            public FileModel AddRecord(FileModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var file = new File
                    {
                        Data = record.File,
                        UniqueId = record.UniqueId,
                        FileName = record.FileName,
                    };

                    domainModel.GetSiteCollection<File>().Add(file);
                    domainModel.SaveChanges();
                    record.FileId = file.FileId;
                    return record;
                }
            }

            public FileModel UpdateRecord(FileModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var file = domainModel.GetSiteCollection<File>().Single(f => f.FileId == record.FileId);

                    file.UniqueId = record.UniqueId;
                    file.FileName = record.FileName;

                    if (record.File != null)
                    {
                        file.Data = record.File;
                    }

                    domainModel.SaveChanges();

                    record.File = null;
                    return record;
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var file in domainModel.GetSiteCollection<File>().Where(f => recordIds.Contains(f.FileId)))
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
        [Column(UserInterfaceTextIds.UniqueId)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.UniqueId, Required = true)]
        public string UniqueId { get; set; }
        [Column(UserInterfaceTextIds.FileName)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.FileName, Required = true)]
        public string FileName { get; set; }
        [InputField(InputFieldType.File, UserInterfaceTextIds.File, Required = true)]
        public byte[] File { get; set; }
    }
}
