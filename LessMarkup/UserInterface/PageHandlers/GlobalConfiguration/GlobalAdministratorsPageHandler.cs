/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Global;
using LessMarkup.UserInterface.PageHandlers.Common;

namespace LessMarkup.UserInterface.PageHandlers.GlobalConfiguration
{
    [ConfigurationHandler(UserInterfaceTextIds.Administrators, IsGlobal = true)]
    public class GlobalAdministratorsPageHandler : RecordListPageHandler<GlobalAdministratorModel>
    {
        public GlobalAdministratorsPageHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }
    }
}
