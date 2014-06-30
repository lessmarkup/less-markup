/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataObjects;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(Collection))]
    public class ModuleModel
    {
        public class Collection : IModelCollection<ModuleModel>
        {
            private long? _siteId;
            private readonly ISiteMapper _siteMapper;

            public Collection(ISiteMapper siteMapper)
            {
                _siteMapper = siteMapper;
            }

            public void Initialize(long? siteId)
            {
                _siteId = siteId;
            }

            public long SiteId
            {
                get
                {
                    if (_siteId.HasValue)
                    {
                        return _siteId.Value;
                    }

                    var siteId = _siteMapper.SiteId;
                    if (siteId.HasValue)
                    {
                        return siteId.Value;
                    }
                    throw new Exception("Unknown site");
                }
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetCollection<Module>().Where(m => m.Enabled && !m.System).Select(m => m.ModuleId);
            }

            public IQueryable<ModuleModel> Read(IDomainModel domainModel, List<long> ids)
            {
                var modules =
                    domainModel.GetCollection<Module>()
                        .Where(m => ids.Contains(m.ModuleId) && m.Enabled && !m.System)
                        .Select(m => new ModuleModel
                        {
                            Name = m.Name,
                            ModuleId = m.ModuleId,
                        }).ToList();

                foreach (var moduleId in domainModel.GetCollection<SiteModule>().Where(s => s.SiteId == SiteId && ids.Contains(s.ModuleId)).Select(s => s.ModuleId))
                {
                    var module = modules.First(m => m.ModuleId == moduleId);
                    module.Enabled = true;
                }

                return modules.AsQueryable();
            }

            public bool Filtered { get { return false; } }
        }

        public long ModuleId { get; set; }

        [Column(UserInterfaceTextIds.Name)]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Enabled)]
        public bool Enabled { get; set; }
    }
}
