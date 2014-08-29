/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.NodeHandlers.Common;
using LessMarkup.UserInterface.NodeHandlers.Configuration;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Sites, IsGlobal = true)]
    public class SiteNodeHandler : RecordListLinkNodeHandler<SiteModel>
    {
        public SiteNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
            : base(domainModelProvider, dataCache, currentUser)
        {
            AddCellLink<SiteCustomizeNodeHandler>(UserInterfaceTextIds.Customize, "customize");
            AddCellLink<SiteUsersNodeHandler>(UserInterfaceTextIds.Users, "users");
            AddCellLink<SiteGroupsNodeHandler>(UserInterfaceTextIds.Groups, "groups");
            AddCellLink<ModulesNodeHandler>(UserInterfaceTextIds.Modules, "modules");
            AddCellLink<NodeListNodeHandler>(UserInterfaceTextIds.Nodes, "nodes");
            AddCellLink<SitePropertiesNodeHandler>(UserInterfaceTextIds.Properties, "properties");
        }
    }
}
