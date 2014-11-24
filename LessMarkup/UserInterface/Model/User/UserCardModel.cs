/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
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
            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                return query.Where("IsRemoved = $", false).ToIdList();
            }

            public int CollectionId
            {
                get
                {
                    return DataHelper.GetCollectionId<DataObjects.Security.User>();
                }
            }

            public IReadOnlyCollection<UserCardModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.Where("IsRemoved = $", false).WhereIds(ids).ToList<UserCardModel>();
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
