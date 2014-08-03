/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Modules)]
    public class ModulesNodeHandler : RecordListNodeHandler<ModuleModel>
    {
        private readonly IChangeTracker _changeTracker;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISiteMapper _siteMapper;

        public ModulesNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker, ISiteMapper siteMapper) : base(domainModelProvider, dataCache)
        {
            _changeTracker = changeTracker;
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;

            AddCellButton(LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.Enable), "enable", "Enabled == false");
            AddCellButton(LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.Disable), "disable", "Enabled == true");
        }

        protected override ModuleModel RecordCommand(long recordId, string commandId)
        {
            var siteId = ObjectId;
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
                if (!siteId.HasValue)
                {
                    throw new Exception("Unknown site");
                }
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                switch (commandId)
                {
                    case "enable":
                    {
                        var siteModule = domainModel.GetCollection<SiteModule>().FirstOrDefault(m => m.SiteId == ObjectId && m.ModuleId == recordId);
                        if (siteModule != null)
                        {
                            break;
                        }
                        siteModule = new SiteModule {ModuleId = recordId, SiteId = siteId.Value};
                        domainModel.GetCollection<SiteModule>().Add(siteModule);
                        _changeTracker.AddChange(siteId.Value, EntityType.Site, EntityChangeType.Updated, domainModel);
                        domainModel.SaveChanges();
                        break;
                    }
                    case "disable":
                    { 
                        var siteModule = domainModel.GetCollection<SiteModule>().FirstOrDefault(m => m.SiteId == siteId.Value && m.ModuleId == recordId);
                        if (siteModule == null)
                        {
                            break;
                        }
                        domainModel.GetCollection<SiteModule>().Remove(siteModule);
                        _changeTracker.AddChange(siteId.Value, EntityType.Site, EntityChangeType.Updated, domainModel);
                        domainModel.SaveChanges();
                        break;
}
                    default:
                        return null;
                }

                var collectionManager = DependencyResolver.Resolve<ModuleModel.Collection>();
                collectionManager.Initialize(ObjectId, AccessType);
                return collectionManager.Read(domainModel, new List<long> {recordId}).ToList()[0];
            }
        }
    }
}
