/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.PageHandlers.Common;

namespace LessMarkup.UserInterface.PageHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Users)]
    public class SiteUsersPageHandler : RecordListPageHandler<UserModel>, IRecordPageHandler
    {
        private long _siteId;

        public SiteUsersPageHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }

        public void Initialize(long siteId)
        {
            _siteId = siteId;
        }

        protected override IModelCollection<UserModel> CreateCollection()
        {
            var collectionManager = (UserModel.Collection) base.CreateCollection();
            collectionManager.Initialize(_siteId);
            return collectionManager;
        }
    }
}
