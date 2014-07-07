/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule
{
    public class RouteConfig
    {
        class RouteExistingControllerConstraint : IRouteConstraint
        {
            private readonly HashSet<string> _controllers; 

            public RouteExistingControllerConstraint(IEnumerable<string> controllers)
            {
                _controllers = new HashSet<string>(controllers);
            }

            public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values,
                RouteDirection routeDirection)
            {
                var controller = (string) values["controller"];

                if (string.IsNullOrWhiteSpace(controller))
                {
                    return false;
                }

                return _controllers.Contains(controller);
            }
        }

        public static void RegisterRoutes(RouteCollection routes, IEngineConfiguration engineConfiguration, IModuleProvider moduleProvider)
        {
            var controllers = new List<string>();

            var controllerBaseType = typeof (Controller);

            foreach (var module in moduleProvider.Modules)
            {
                controllers.AddRange(module.Assembly.GetTypes()
                    .Where(controllerBaseType.IsAssignableFrom)
                    .Select(t => t.Name.EndsWith("Controller") ? 
                        t.Name.Substring(0, t.Name.Length-"Controller".Length) : 
                        t.Name));
            }

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: Constants.Routes.DefaultRoute,
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    action = Constants.Routes.DefaultAction, 
                    id = UrlParameter.Optional
                },
                constraints: new
                {
                    validatController = new RouteExistingControllerConstraint(controllers)
                }
            );
        }
    }
}