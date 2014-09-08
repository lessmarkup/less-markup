/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Forum.Model;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class AllForumsNodeHandler : AbstractNodeHandler
    {
        protected override Dictionary<string, object> GetViewData()
        {
            var statistics = DependencyResolver.Resolve<AllForumsStatistics>();

            if (ObjectId.HasValue)
            {
                statistics.CollectStatistics(ObjectId.Value, typeof(ForumNodeHandler));
                statistics.OrganizeGroups(true);
            }

            return new Dictionary<string, object>
            {
                { "Groups", statistics.Groups }
            };
        }
    }
}
