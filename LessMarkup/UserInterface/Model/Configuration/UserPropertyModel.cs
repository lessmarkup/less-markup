using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Security;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.Model.Configuration
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.UserProperty, CollectionType = typeof(Collection))]
    public class UserPropertyModel
    {
        public class Collection : IEditableModelCollection<UserPropertyModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public Collection(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _changeTracker = changeTracker;
                _domainModelProvider = domainModelProvider;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return RecordListHelper.GetFilterAndOrderQuery(domainModel.GetSiteCollection<UserPropertyDefinition>(), filter, typeof (UserPropertyModel)).Select(d => d.Id);
            }

            public int CollectionId
            {
                get { return DataHelper.GetCollectionId<UserPropertyDefinition>(); }
            }

            public IQueryable<UserPropertyModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return
                    domainModel.GetSiteCollection<UserPropertyDefinition>()
                        .Where(d => ids.Contains(d.Id))
                        .Select(d => new UserPropertyModel
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Title = d.Title,
                            Type = d.Type
                        });
            }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public UserPropertyModel CreateRecord()
            {
                return new UserPropertyModel();
            }

            public void AddRecord(UserPropertyModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var definition = new UserPropertyDefinition
                    {
                        Name = record.Name,
                        Title = record.Title,
                        Type = record.Type
                    };

                    domainModel.GetSiteCollection<UserPropertyDefinition>().Add(definition);
                    domainModel.SaveChanges();

                    _changeTracker.AddChange(definition, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();

                    record.Id = definition.Id;
                }
            }

            public void UpdateRecord(UserPropertyModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var definition = domainModel.GetSiteCollection<UserPropertyDefinition>().First(d => d.Id == record.Id);

                    definition.Name = record.Name;
                    definition.Title = record.Title;
                    definition.Type = record.Type;

                    _changeTracker.AddChange(definition, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var record in domainModel.GetSiteCollection<UserPropertyDefinition>().Where(d => recordIds.Contains(d.Id)))
                    {
                        domainModel.GetSiteCollection<UserPropertyDefinition>().Remove(record);
                        _changeTracker.AddChange(record, EntityChangeType.Removed, domainModel);
                    }
                    domainModel.SaveChanges();
                }
                return true;
            }

            public bool DeleteOnly { get { return false; } }
        }

        public long Id { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Name, Required = true)]
        [Column(UserInterfaceTextIds.Name)]
        public string Name { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Title, Required = true)]
        [Column(UserInterfaceTextIds.Title)]
        public string Title { get; set; }

        [InputField(InputFieldType.Select, UserInterfaceTextIds.Type, Required = true)]
        [Column(UserInterfaceTextIds.Type)]
        public UserPropertyType Type { get; set; }
    }
}
