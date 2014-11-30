/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Forum.Model;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class AllForumsNodeHandler : AbstractNodeHandler
    {
        private readonly IDataCache _dataCache;

        public AllForumsNodeHandler(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var summaryProvider = DependencyResolver.Resolve<ForumSummaryProvider>();

            if (ObjectId.HasValue)
            {
                summaryProvider.CollectStatistics(ObjectId.Value, typeof(ForumNodeHandler));
                summaryProvider.OrganizeGroups(true);
            }

            var ret = new Dictionary<string, object>
            {
                { "groups", summaryProvider.Groups }
            };

            var settings = GetSettings<AllForumsSettingsModel>();

            if (settings == null || settings.ShowStatistics)
            {
                CalculateStatistics(ret);
            }

            return ret;
        }

        private void CalculateStatistics(Dictionary<string, object> result)
        {
            var moduleStatistics = _dataCache.Get<ModuleStatisticsCache>();

            result["showStatistics"] = true;
            result["activeUsers"] = moduleStatistics.ActiveUsers;
            result["statistics"] = moduleStatistics.Forums;
        }

        protected override Type SettingsModel
        {
            get { return typeof(AllForumsSettingsModel); }
        }
    }
}
