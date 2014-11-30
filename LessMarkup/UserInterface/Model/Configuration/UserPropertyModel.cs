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
    [RecordModel(TitleTextId = UserInterfaceTextIds.UserProperty, CollectionType = typeof(Collection), DataType = typeof(UserPropertyDefinition))]
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

            public IReadOnlyCollection<long> ReadIds(IQueryBuilder query, bool ignoreOrder)
            {
                return query.ToIdList();
            }

            public int CollectionId
            {
                get { return DataHelper.GetCollectionId<UserPropertyDefinition>(); }
            }

            public IReadOnlyCollection<UserPropertyModel> Read(IQueryBuilder queryBuilder, List<long> ids)
            {
                return queryBuilder
                    .WhereIds(ids)
                    .ToList<UserPropertyDefinition>()
                        .Select(d => new UserPropertyModel
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Title = d.Title,
                            Type = d.Type
                        }).ToList();
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

                    domainModel.Create(definition);
                    _changeTracker.AddChange(definition, EntityChangeType.Added, domainModel);

                    record.Id = definition.Id;
                }
            }

            public void UpdateRecord(UserPropertyModel record)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var definition = domainModel.Query().Find<UserPropertyDefinition>(record.Id);

                    definition.Name = record.Name;
                    definition.Title = record.Title;
                    definition.Type = record.Type;

                    domainModel.Update(definition);

                    _changeTracker.AddChange(definition, EntityChangeType.Updated, domainModel);
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var id in recordIds)
                    {
                        domainModel.Delete<UserPropertyDefinition>(id);
                        _changeTracker.AddChange<UserPropertyDefinition>(id, EntityChangeType.Removed, domainModel);
                    }
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
