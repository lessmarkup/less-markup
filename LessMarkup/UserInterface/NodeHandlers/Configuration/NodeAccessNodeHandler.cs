/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.UserInterface.Model.Configuration;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.Configuration
{
    public class NodeAccessNodeHandler : RecordListNodeHandler<NodeAccessModel>, IPropertyCollectionManager
    {
        private long _nodeId;

        public NodeAccessNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }

        public void Initialize(long nodeId)
        {
            _nodeId = nodeId;
        }

        protected override IModelCollection<NodeAccessModel> CreateCollection()
        {
            var collectionManager = (NodeAccessModel.CollectionManager) base.CreateCollection();
            collectionManager.Initialize(_nodeId);
            return collectionManager;
        }

        public IReadOnlyCollection<string> GetCollection(IDomainModel domainModel, string property, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentOutOfRangeException("searchText");
            }

            searchText = "%" + searchText + "%";

            switch (property)
            {
                case "user":
                    return domainModel.Query().From<DataObjects.Security.User>().Where("Name LIKE $ OR Email LIKE $", searchText, searchText).ToList<DataObjects.Security.User>("Email").Select(u => u.Email).ToList();
                case "group":
                    return domainModel.Query().From<UserGroup>().Where("Name LIKE $", searchText).ToList<UserGroup>("Name").Select(g => g.Name).ToList();
                default:
                    return null;
            }
        }
    }
}
