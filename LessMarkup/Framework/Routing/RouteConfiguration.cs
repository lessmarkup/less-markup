/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Reflection;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using LessMarkup.Interfaces.Module;
using DependencyResolver = LessMarkup.DataFramework.DependencyResolver;

namespace LessMarkup.Framework.Routing
{
    public class RouteConfiguration
    {
        private readonly IModuleProvider _moduleProvider;

        public RouteConfiguration(IModuleProvider moduleProvider)
        {
            _moduleProvider = moduleProvider;
        }

        private void AddRoute(RouteCollection routes, string routeName, string pattern, string controllerName, string actionName, string defaultsText, string constraintsText, Type[] constraintTypes)
        {
            var defaults = new RouteValueDictionary
                    {
                        {"controller", controllerName},
                        {"action", actionName}
                    };

            var constraints = new RouteValueDictionary();

            if (!string.IsNullOrWhiteSpace(defaultsText))
            {
                object defaultsObject = Json.Decode(defaultsText);
                foreach (var property in defaultsObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var name = property.Name;
                    var value = property.GetValue(defaultsObject);
                    defaults.Add(name, value);
                }
            }

            if (!string.IsNullOrWhiteSpace(constraintsText))
            {
                foreach (var item in Json.Decode(constraintsText))
                {
                    var constraint = item.Value;

                    if (constraint is string)
                    {
                        constraints.Add(item.Key, constraint);
                        continue;
                    }

                    var className = constraint["class"];
                    if (string.IsNullOrWhiteSpace(className))
                    {
                        continue;
                    }

                    var constraintTargetType = Type.GetType(className, false);
                    if (constraintTargetType == null)
                    {
                        continue;
                    }

                    var constraintTarget = DependencyResolver.Resolve(constraintTargetType);
                    foreach (var property in constraintTargetType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (!property.CanWrite)
                        {
                            continue;
                        }
                        property.SetValue(constraintTarget, constraint[property.Name]);
                    }

                    constraints.Add(item.Key, constraintTarget);
                }
            }

            if (constraintTypes != null)
            {
                foreach (var constraintType in constraintTypes)
                {
                    var constraintObject = DependencyResolver.Resolve(constraintType);
                    if (constraintObject != null)
                    {
                        constraints.Add(constraintType.FullName, constraintObject);
                    }
                }
            }

            var existing = routes[routeName];
            if (existing != null)
            {
                routes.Remove(existing);
            }
            routes.MapRoute(routeName, pattern, defaults, constraints);
        }

        public void Create(RouteCollection routes)
        {
            foreach (var module in _moduleProvider.Modules)
            foreach (var controller in module.Assembly.GetTypes().Where(t => typeof(Controller).IsAssignableFrom(t)))
            {
                var controllerName = controller.Name;
                if (controllerName.EndsWith("Controller"))
                {
                    controllerName = controllerName.Remove(controllerName.Length - "Controller".Length);
                }

                foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (var routeAttribute in action.GetCustomAttributes<RouteAttribute>())
                {
                    AddRoute(routes, routeAttribute.Name, routeAttribute.Pattern, controllerName, action.Name, 
                        routeAttribute.Defaults, routeAttribute.Constraints, routeAttribute.ConstraintTypes);
                }
            }
        }
    }
}
