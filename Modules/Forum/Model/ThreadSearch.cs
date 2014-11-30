/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Forum.Model
{
    public class ThreadSearch : IEntitySearch
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public ThreadSearch(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public string ValidateAndGetUrl(int collectionId, long entityId, IDomainModel domainModel)
        {
            var thread = domainModel.Query().Find<Thread>(entityId);
            if (thread == null)
            {
                return null;
            }

            var nodeCache = _dataCache.Get<INodeCache>();

            var node = nodeCache.GetNode(thread.ForumId);

            if (node == null || node.CheckRights(_currentUser) == NodeAccessType.NoAccess)
            {
                return null;
            }

            return string.Format("{0}/{1}", node.FullPath, thread.Path);
        }

        public string GetFriendlyName(int collectionId)
        {
            return LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.ThreadName);
        }
    }
}
