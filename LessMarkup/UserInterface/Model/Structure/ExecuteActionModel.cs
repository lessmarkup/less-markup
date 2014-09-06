/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using LessMarkup.Engine.Helpers;
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

        public object HandleRequest(Dictionary<string, object> data, string path)
        {
            path = HttpUtility.UrlDecode(path);

            if (path != null)
            {
                var queryPost = path.IndexOf('?');
                if (queryPost >= 0)
                {
                    path = path.Substring(0, queryPost);
                }
            }

            var nodeCache = _dataCache.Get<INodeCache>();
            var handler = nodeCache.GetNodeHandler(path);
            var handlerType = handler.GetType();

            var actionName = data["command"].ToString();

            var method = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault(m => string.Compare(m.Name, actionName, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (method == null)
            {
                throw new UnknownActionException();
            }

            var accessAttribute = method.GetCustomAttribute<ActionAccessAttribute>(true);

            if (accessAttribute != null)
            {
                if ((int) handler.AccessType < (int) accessAttribute.MinimumAccess)
                {
                    throw new ActionAccessException();
                }
            }

            var parameters = method.GetParameters();

            var arguments = new object[parameters.Length];

            var dataLowered = data.ToDictionary(k => k.Key.ToLower(), k => k.Value);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterName = parameters[i].Name.ToLower();
                var parameterType = parameters[i].ParameterType;

                object parameter;

                if (!dataLowered.TryGetValue(parameterName, out parameter))
                {
                    if (parameterName.StartsWith("raw") && parameterType == typeof(string) && dataLowered.TryGetValue(parameterName.Substring(3), out parameter))
                    {
                        arguments[i] = parameter != null ? JsonConvert.SerializeObject(parameter) : null;
                    }

                    continue;
                }

                if (parameter.GetType() == parameterType)
                {
                    arguments[i] = parameter;
                }
                else if (parameterType.IsPrimitive || !parameterType.IsClass)
                {
                    arguments[i] = Convert.ChangeType(parameter, parameterType);
                }
                else
                {
                    var serialized = JsonConvert.SerializeObject(parameter);
                    arguments[i] = JsonHelper.ResolveAndDeserializeObject(serialized, parameterType);
                }
            }

            handler.Context = dataLowered;

            return method.Invoke(handler, arguments);
        }
    }
}
