/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using System.Web.Routing;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes, IEngineConfiguration engineConfiguration)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: Constants.Routes.GlobalRoute,
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = Constants.Routes.GlobalController,
                    action = Constants.Routes.DefaultAction,
                    id = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: Constants.Routes.DefaultRoute,
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = Constants.Routes.DefaultController, 
                    action = Constants.Routes.DefaultAction, 
                    id = UrlParameter.Optional
                }
            );
        }
    }
}