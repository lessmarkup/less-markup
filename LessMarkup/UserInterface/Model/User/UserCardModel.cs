/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(CollectionType = typeof(Collection), DataType = typeof(DataObjects.Security.User))]
    public class UserCardModel
    {
        public class Collection : IModelCollection<UserCardModel>
        {
            public IReadOnlyCollection<long> ReadIds(IQueryBuilder query, bool ignoreOrder)
            {
                return query.From<DataObjects.Security.User>().Where("IsRemoved = $", false).ToIdList();
            }

            public int CollectionId
            {
                get
                {
                    return DataHelper.GetCollectionId<DataObjects.Security.User>();
                }
            }

            public IReadOnlyCollection<UserCardModel> Read(IQueryBuilder query, List<long> ids)
            {
                return query.From<DataObjects.Security.User>()
                    .Where("IsRemoved = $", false)
                    .WhereIds(ids)
                    .ToList<DataObjects.Security.User>()
                    .Select(u => new UserCardModel
                    {
                        Name = u.Name,
                        UserId = u.Id,
                        Signature = u.Signature,
                        Title = u.Title
                    }).ToList();
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }
        }

        public long UserId { get; set; }

        [Column(UserInterfaceTextIds.Name, CellUrl = "{userId}")]
        [RecordSearch]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Title)]
        [RecordSearch]
        public string Title { get; set; }

        [Column(UserInterfaceTextIds.Signature)]
        [RecordSearch]
        public string Signature { get; set; }
    }
}
