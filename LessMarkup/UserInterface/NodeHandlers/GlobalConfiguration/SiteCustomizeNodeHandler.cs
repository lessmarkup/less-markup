/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Customize)]
    public class SiteCustomizeNodeHandler : RecordListNodeHandler<CustomizationModel>, IRecordNodeHandler
    {
        private long? _siteId;

        public SiteCustomizeNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }

        public void Initialize(long recordId)
        {
            _siteId = recordId;
        }

        protected override IModelCollection<CustomizationModel> CreateCollection()
        {
            var collectionManager = (CustomizationModel.CollectionManager) base.CreateCollection();
            collectionManager.Initialize(_siteId);
            return collectionManager;
        }
    }
}
