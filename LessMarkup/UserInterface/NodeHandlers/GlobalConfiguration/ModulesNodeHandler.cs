/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Modules)]
    public class ModulesNodeHandler : NewRecordListNodeHandler<ModuleModel>
    {
        private readonly IChangeTracker _changeTracker;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISiteMapper _siteMapper;

        public ModulesNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker, ISiteMapper siteMapper, ICurrentUser currentUser) : base(domainModelProvider, dataCache, currentUser)
        {
            _changeTracker = changeTracker;
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;
        }

        [RecordAction(UserInterfaceTextIds.Enable, Visible = "!Enabled")]
        public object EnableModule(long recordId, string filter)
        {
            return EnableModule(recordId, true);
        }

        [RecordAction(UserInterfaceTextIds.Disable, Visible = "Enabled")]
        public object DisableModule(long recordId, string filter)
        {
            return EnableModule(recordId, false);
        }

        protected object EnableModule(long moduleId, bool enable)
        {
            long siteId;
            if (ObjectId.HasValue)
            {
                siteId = ObjectId.Value;
            }
            else
            {
                if (!_siteMapper.SiteId.HasValue)
                {
                    throw new Exception("Unknown site");
                }
                siteId = _siteMapper.SiteId.Value;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var siteModule = domainModel.GetCollection<SiteModule>().FirstOrDefault(m => m.SiteId == ObjectId && m.ModuleId == moduleId);

                if (enable)
                {
                    if (siteModule == null)
                    {
                        siteModule = new SiteModule
                        {
                            ModuleId = moduleId, 
                            SiteId = siteId
                        };
                        domainModel.GetCollection<SiteModule>().Add(siteModule);
                        _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);
                        domainModel.SaveChanges();
                    }
                }
                else
                {
                    if (siteModule != null)
                    {
                        domainModel.GetCollection<SiteModule>().Remove(siteModule);
                        _changeTracker.AddChange<Site>(siteId, EntityChangeType.Updated, domainModel);
                        domainModel.SaveChanges();
                    }
                }

                var collectionManager = DependencyResolver.Resolve<ModuleModel.Collection>();
                collectionManager.Initialize(ObjectId, AccessType);

                var record = collectionManager.Read(domainModel, new List<long> {moduleId}).ToList()[0];

                return new
                {
                    record,
                    index = GetIndex(record, null, domainModel)
                };
            }
        }
    }
}
