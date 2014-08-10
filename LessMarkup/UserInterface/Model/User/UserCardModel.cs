using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel]
    public class UserCardModel
    {
        public class Collection : IModelCollection<UserCardModel>
        {
            private readonly ISiteMapper _siteMapper;

            public Collection(ISiteMapper siteMapper)
            {
                _siteMapper = siteMapper;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                var siteId = _siteMapper.SiteId;

                if (!siteId.HasValue)
                {
                    return new EnumerableQuery<long>(new long[0]);
                }

                return
                    domainModel.GetCollection<DataObjects.User.User>()
                        .Where(u => !u.IsRemoved && u.SiteId == siteId.Value)
                        .Select(u => u.UserId);
            }

            public IQueryable<UserCardModel> Read(IDomainModel domainModel, List<long> ids)
            {
                var siteId = _siteMapper.SiteId;

                if (!siteId.HasValue)
                {
                    return new EnumerableQuery<UserCardModel>(new UserCardModel[0]);
                }

                return domainModel.GetCollection<DataObjects.User.User>()
                    .Where(u => !u.IsRemoved && u.SiteId == siteId.Value && ids.Contains(u.UserId))
                    .Select(u => new UserCardModel
                    {
                        Name = u.Name,
                        Signature = u.Signature,
                        Title = u.Title, 
                        UserId = u.UserId
                    });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }
        }

        public long UserId { get; set; }

        [Column(UserInterfaceTextIds.Name, CellUrl = "{UserId}")]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Title)]
        public string Title { get; set; }

        [Column(UserInterfaceTextIds.Signature)]
        public string Signature { get; set; }
    }
}
