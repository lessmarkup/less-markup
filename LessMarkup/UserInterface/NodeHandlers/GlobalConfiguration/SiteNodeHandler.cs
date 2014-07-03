/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;
using LessMarkup.UserInterface.NodeHandlers.Configuration;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Sites, IsGlobal = true)]
    public class SiteNodeHandler : RecordListLinkNodeHandler<SiteModel>
    {
        public SiteNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
            AddCellLink<SiteCustomizeNodeHandler>("Customize", "customize");
            AddCellLink<SiteUsersNodeHandler>("Users", "users");
            AddCellLink<SiteGroupsNodeHandler>("Groups", "groups");
            AddCellLink<ModulesNodeHandler>("Modules", "modules");
            AddCellLink<NodeListNodeHandler>("Nodes", "nodes");
            AddCellLink<SitePropertiesNodeHandler>("Properties", "properties");
        }
    }
}
