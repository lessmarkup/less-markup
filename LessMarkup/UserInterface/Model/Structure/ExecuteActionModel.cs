﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var nodeCache = _dataCache.Get<NodeCache>();

            CachedNodeInformation node;
            string rest;
            nodeCache.GetNode(path, out node, out rest);
            if (node == null)
            {
                throw new UnknownActionException();
            }

            var handler = (INodeHandler) DependencyResolver.Resolve(node.HandlerType);
            string settings = node.Settings;

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

            var method = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
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

                string parameterText;
                if (!dataLowered.TryGetValue(parameterName, out parameterText))
                {
                    if (parameterName == "settings")
                    {
                        arguments[i] = settings != null ? JsonConvert.DeserializeObject<Dictionary<string, string>>(settings) : null;
                    }

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

                    var valueData = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameterText);

                    foreach (var property in parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        object propertyValue;
                        if (!valueData.TryGetValue(property.Name, out propertyValue) || propertyValue == null)
                        {
                            continue;
                        }

                        if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(value, propertyValue);
                        }
                        else if (property.PropertyType == typeof (bool))
                        {
                            property.SetValue(value, propertyValue);
                        }
                        else
                        {
                            property.SetValue(value, JsonConvert.DeserializeObject(propertyValue.ToString(), property.PropertyType));
                        }
                    }
                }

                arguments[i] = value;
            }

            return method.Invoke(handler, arguments);
        }
    }
}
