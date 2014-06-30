/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web;
using System.Web.Routing;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Site
{
    public class GlobalRuleConstraint : IRouteConstraint
    {
        private readonly ISiteMapper _siteMapper;

        public GlobalRuleConstraint(ISiteMapper siteMapper)
        {
            _siteMapper = siteMapper;
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            return !_siteMapper.SiteId.HasValue;
        }
    }
}
