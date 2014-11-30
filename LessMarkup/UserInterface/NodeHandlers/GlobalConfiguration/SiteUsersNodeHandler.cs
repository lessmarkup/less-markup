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
    [ConfigurationHandler(UserInterfaceTextIds.Users)]
    public class SiteUsersNodeHandler : RecordListNodeHandler<UserModel>
    {
        private readonly IDomainModelProvider _domainModelProvider;

        public SiteUsersNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
            _domainModelProvider = domainModelProvider;
        }

        [RecordAction(UserInterfaceTextIds.BlockUser, Visible = "!isBlocked", CreateType = typeof(UserBlockModel))]
        public object Block(long recordId, UserBlockModel newObject)
        {
            newObject.BlockUser(recordId);

            using (var domainModel = _domainModelProvider.Create())
            {
                var users = GetCollection().Read(domainModel.Query(), new List<long> {recordId});
                return ReturnRecordResult(users.Single());
            }
        }

        [RecordAction(UserInterfaceTextIds.Unblock, Visible = "isBlocked")]
        public object Unblock(long recordId)
        {
            var model = DependencyResolver.Resolve<UserBlockModel>();
            model.UnblockUser(recordId);

            using (var domainModel = _domainModelProvider.Create())
            {
                var users = GetCollection().Read(domainModel.Query(), new List<long> { recordId });
                return ReturnRecordResult(users.Single());
            }
        }
    }
}
