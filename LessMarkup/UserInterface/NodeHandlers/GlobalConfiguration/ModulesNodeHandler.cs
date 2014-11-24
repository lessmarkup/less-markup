/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Modules)]
    public class ModulesNodeHandler : RecordListNodeHandler<ModuleModel>
    {
        private readonly IChangeTracker _changeTracker;
        private readonly ILightDomainModelProvider _domainModelProvider;

        public ModulesNodeHandler(ILightDomainModelProvider domainModelProvider, IDataCache dataCache, IChangeTracker changeTracker) : base(domainModelProvider, dataCache)
        {
            _changeTracker = changeTracker;
            _domainModelProvider = domainModelProvider;
        }

        [RecordAction(UserInterfaceTextIds.Enable, Visible = "!enabled")]
        public object EnableModule(long recordId, string filter)
        {
            return EnableModule(recordId, true);
        }

        [RecordAction(UserInterfaceTextIds.Disable, Visible = "enabled")]
        public object DisableModule(long recordId, string filter)
        {
            return EnableModule(recordId, false);
        }

        protected object EnableModule(long moduleId, bool enable)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var siteModule = domainModel.Query().Find<Module>(moduleId);

                siteModule.Enabled = enable;
                domainModel.Update(siteModule);

                var collectionManager = DependencyResolver.Resolve<ModuleModel.Collection>();
                collectionManager.Initialize(ObjectId, AccessType);

                var record = collectionManager.Read(domainModel.Query(), new List<long> {moduleId}).ToList()[0];

                return new
                {
                    record,
                    index = GetIndex(record, null, domainModel)
                };
            }
        }
    }
}
