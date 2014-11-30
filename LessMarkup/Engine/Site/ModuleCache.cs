/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.Site
{
    public class ModuleCache : AbstractCacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleProvider _moduleProvider;

        public HashSet<string> ModuleTypes { get; private set; }

        public ModuleCache(IDomainModelProvider domainModelProvider, IModuleProvider moduleProvider)
            : base(new[] { typeof(DataObjects.Common.Module)})
        {
            _domainModelProvider = domainModelProvider;
            _moduleProvider = moduleProvider;
        }

        private IEnumerable<string> GetSystemModuleTypes()
        {
            return _moduleProvider.Modules.Where(m => m.System).Select(m => m.ModuleType).ToList();
        }

        protected override void Initialize(long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            var systemModuleTypes = GetSystemModuleTypes();

            using (var domainModel = _domainModelProvider.Create())
            {
                ModuleTypes = new HashSet<string>(domainModel.Query().From<DataObjects.Common.Module>().Where("Enabled = $", true).ToList<DataObjects.Common.Module>().Select(m => m.ModuleType));

                foreach (var moduleType in systemModuleTypes)
                {
                    if (!ModuleTypes.Contains(moduleType))
                    {
                        ModuleTypes.Add(moduleType);
                    }
                }
            }
        }
    }
}
