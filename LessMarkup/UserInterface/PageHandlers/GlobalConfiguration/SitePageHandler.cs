/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.PageHandlers.Common;
using LessMarkup.UserInterface.PageHandlers.Configuration;

namespace LessMarkup.UserInterface.PageHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Sites, IsGlobal = true)]
    public class SitePageHandler : RecordListLinkPageHandler<SiteModel>
    {
        public SitePageHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
            AddCellLink<SiteCustomizePageHandler>("Customize", "customize");
            AddCellLink<SiteUsersPageHandler>("Users", "users");
            AddCellLink<SiteGroupsPageHandler>("Groups", "groups");
            AddCellLink<ModulesPageHandler>("Modules", "modules");
            AddCellLink<NodeListPageHandler>("Nodes", "nodes");
        }
    }
}
