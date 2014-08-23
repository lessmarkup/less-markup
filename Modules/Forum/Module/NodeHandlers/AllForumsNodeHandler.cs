using System;
using System.Collections.Generic;
using System.Linq;
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
