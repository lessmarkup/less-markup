/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Initialization
{
    public class NodeSearch : IEntitySearch
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public NodeSearch(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public string ValidateAndGetUrl(int collectionId, long entityId, ILightDomainModel domainModel)
        {
            var nodeCache = _dataCache.Get<INodeCache>();

            var node = nodeCache.GetNode(entityId);

            if (node == null)
            {
                return null;
            }

            var rights = node.CheckRights(_currentUser);

            if (rights == NodeAccessType.NoAccess)
            {
                return null;
            }

            return node.FullPath;
        }

        public string GetFriendlyName(int collectionId)
        {
            return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.NodeName);
        }
    }
}
