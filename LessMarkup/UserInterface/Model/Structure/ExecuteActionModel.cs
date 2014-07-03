/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.Framework.Helpers;
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

                arguments[i] = JsonHelper.ResolveAndDeserializeObject(parameterText, parameters[i].ParameterType);
            }

            return method.Invoke(handler, arguments);
        }
    }
}
