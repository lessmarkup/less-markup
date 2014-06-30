/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Exceptions;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class ExecuteActionModel
    {
        private readonly IDataCache _dataCache;

        public ExecuteActionModel(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public object HandleRequest(Dictionary<string, string> data)
        {
            var path = data["-path-"];

            var pageCache = _dataCache.Get<PageCache>();

            CachedPageInformation page;
            string rest;
            pageCache.GetPage(path, out page, out rest);
            if (page == null)
            {
                throw new UnknownActionException();
            }

            var handler = (IPageHandler) DependencyResolver.Resolve(page.HandlerType);
            string settings = page.Settings;

            while (!string.IsNullOrWhiteSpace(rest))
            {
                var childSettings = handler.GetChildHandler(rest);
                if (childSettings == null || !childSettings.Id.HasValue)
                {
                    throw new UnknownActionException();
                }
                settings = null;
                handler = childSettings.Handler;
                rest = childSettings.Rest;
            }

            var handlerType = handler.GetType();

            var actionName = data["-action-"];

            var method =
                handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .SingleOrDefault(m => string.Compare(m.Name, actionName, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (method == null)
            {
                throw new UnknownActionException();
            }

            var parameters = method.GetParameters();

            var arguments = new object[parameters.Length];

            var dataLowered = data.ToDictionary(k => k.Key.ToLower(), k => k.Value);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterName = parameters[i].Name.ToLower();

                if (parameterName == "settings")
                {
                    arguments[i] = JsonConvert.DeserializeObject<Dictionary<string, string>>(settings);
                    continue;
                }

                string parameterText;
                if (!dataLowered.TryGetValue(parameterName, out parameterText))
                {
                    continue;
                }

                object value;

                var parameterType = parameters[i].ParameterType;

                if (parameterType == typeof (string))
                {
                    value = parameterText;
                }
                else if (parameterType == typeof (bool))
                {
                    value = !string.IsNullOrWhiteSpace(parameterText) && bool.Parse(parameterText);
                }
                else if (!parameterType.IsClass || parameterType == typeof(DateTime) || parameterType.GetConstructor(new Type[0]) != null)
                {
                    value = string.IsNullOrWhiteSpace(parameterText) ? null : JsonConvert.DeserializeObject(parameterText, parameterType);
                }
                else
                {
                    value = DependencyResolver.Resolve(parameterType);

                    var valueData = JsonConvert.DeserializeObject<Dictionary<string, string>>(parameterText);

                    foreach (var property in parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        string propertyValue;
                        if (!valueData.TryGetValue(property.Name, out propertyValue) || string.IsNullOrWhiteSpace(propertyValue))
                        {
                            continue;
                        }

                        if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(value, propertyValue);
                        }
                        else if (property.PropertyType == typeof (bool))
                        {
                            property.SetValue(value, bool.Parse(propertyValue));
                        }
                        else
                        {
                            property.SetValue(value, JsonConvert.DeserializeObject(propertyValue, property.PropertyType));
                        }
                    }
                }

                arguments[i] = value;
            }

            return method.Invoke(handler, arguments);
        }
    }
}
