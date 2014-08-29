using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(CollectionType = typeof(Collection), DataType = typeof(DataObjects.Security.User))]
    public class UserCardModel
    {
        public class Collection : IModelCollection<UserCardModel>
        {
            private readonly ISiteMapper _siteMapper;

            public Collection(ISiteMapper siteMapper)
            {
                _siteMapper = siteMapper;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                var siteId = _siteMapper.SiteId;

                if (!siteId.HasValue)
                {
                    return new EnumerableQuery<long>(new long[0]);
                }

                var collection = RecordListHelper.GetFilterAndOrderQuery(domainModel.GetCollection<DataObjects.Security.User>(), filter, typeof (UserCardModel));

                return collection
                        .Where(u => !u.IsRemoved && u.SiteId == siteId.Value)
                        .Select(u => u.Id);
            }

            public int CollectionId { get { return AbstractDomainModel.GetCollectionId<DataObjects.Security.User>(); } }

            public IQueryable<UserCardModel> Read(IDomainModel domainModel, List<long> ids)
            {
                var siteId = _siteMapper.SiteId;

                if (!siteId.HasValue)
                {
                    return new EnumerableQuery<UserCardModel>(new UserCardModel[0]);
                }

                return domainModel.GetCollection<DataObjects.Security.User>()
                    .Where(u => !u.IsRemoved && u.SiteId == siteId.Value && ids.Contains(u.Id))
                    .Select(u => new UserCardModel
                    {
                        Name = u.Name,
                        Signature = u.Signature,
                        Title = u.Title, 
                        UserId = u.Id
                    });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }
        }

        public long UserId { get; set; }

        [Column(UserInterfaceTextIds.Name, CellUrl = "{UserId}")]
        [UseInSearch]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Title)]
        [UseInSearch]
        public string Title { get; set; }

        [Column(UserInterfaceTextIds.Signature)]
        [UseInSearch]
        public string Signature { get; set; }
    }
}
