/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Configuration;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.Configuration
{
    public class NodeAccessNodeHandler : NewRecordListNodeHandler<NodeAccessModel>, IPropertyCollectionManager
    {
        private readonly ISiteMapper _siteMapper;
        private long? _siteId;
        private long _nodeId;

        public NodeAccessNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ISiteMapper siteMapper, ICurrentUser currentUser)
            : base(domainModelProvider, dataCache, currentUser)
        {
            _siteMapper = siteMapper;
        }

        public void Initialize(long? siteId, long nodeId)
        {
            _siteId = siteId;
            _nodeId = nodeId;
        }

        protected override IModelCollection<NodeAccessModel> CreateCollection()
        {
            var collectionManager = (NodeAccessModel.CollectionManager) base.CreateCollection();
            collectionManager.Initialize(_siteId, _nodeId);
            return collectionManager;
        }

        public IQueryable<string> GetCollection(IDomainModel domainModel, string property, string searchText)
        {
            var siteId = _siteId ?? _siteMapper.SiteId;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentOutOfRangeException("searchText");
            }

            searchText = "%" + searchText + "%";

            switch (property)
            {
                case "User":
                    if (!siteId.HasValue)
                    {
                        throw new NullReferenceException();
                    }
                    return domainModel.GetCollection<DataObjects.Security.User>().Where(u => u.SiteId == siteId.Value && (SqlFunctions.PatIndex(searchText, u.Name) > 0 || SqlFunctions.PatIndex(searchText, u.Email) > 0)).Select(u => u.Email);
                case "Group":
                    return domainModel.GetSiteCollection<UserGroup>(_siteId).Where(g => SqlFunctions.PatIndex(searchText, g.Name) > 0).Select(g => g.Name);
                default:
                    return null;
            }
        }
    }
}
