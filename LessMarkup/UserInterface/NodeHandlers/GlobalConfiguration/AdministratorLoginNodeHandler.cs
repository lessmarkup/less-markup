/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    public class AdministratorLoginNodeHandler : AbstractNodeHandler
    {
        private readonly IDataCache _dataCache;
        
        public AdministratorLoginNodeHandler(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var siteConfiguration = _dataCache.Get<ISiteConfiguration>();
            var adminLoginPage = siteConfiguration.AdminLoginPage;

            return new Dictionary<string, object>
            {
                {
                    "AdministratorKey",
                    adminLoginPage
                }
            };
        }
    }
}
