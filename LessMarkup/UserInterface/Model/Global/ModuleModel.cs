/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(Collection), DataType = typeof(Module))]
    public class ModuleModel
    {
        public class Collection : IModelCollection<ModuleModel>
        {
            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                query = query.From<Module>().Where("Removed = $ AND System = $", false, false);
                return query.ToIdList();
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<Module>(); } }

            public IReadOnlyCollection<ModuleModel> Read(ILightQueryBuilder query, List<long> ids)
            {
                return query.From<Module>().Where("Removed = $ AND System = $", false, false).WhereIds(ids).ToList<Module>()
                        .Select(m => new ModuleModel
                        {
                            Name = m.Name,
                            ModuleId = m.Id,
                            Enabled = m.Enabled
                        }).ToList();
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }
        }

        public long ModuleId { get; set; }

        [Column(UserInterfaceTextIds.Name)]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Enabled)]
        public bool Enabled { get; set; }
    }
}
