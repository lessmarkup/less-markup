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
    [ConfigurationHandler(UserInterfaceTextIds.Groups)]
    public class SiteGroupsNodeHandler: RecordListNodeHandler<UserGroupModel>, IRecordNodeHandler
    {
        private long _siteId;

        public SiteGroupsNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }

        public void Initialize(long siteId)
        {
            _siteId = siteId;
        }

        protected override IModelCollection<UserGroupModel> CreateCollection()
        {
            var collectionManager = (UserGroupModel.Collection) base.CreateCollection();
            collectionManager.Initialize(_siteId);
            return collectionManager;
        }
    }

}
