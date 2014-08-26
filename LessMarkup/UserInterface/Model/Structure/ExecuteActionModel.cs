/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.Engine.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Exceptions;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class ExecuteActionModel
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public ExecuteActionModel(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public object HandleRequest(Dictionary<string, object> data, System.Web.Mvc.Controller controller)
        {
            var path = data["-path-"].ToString();

            var nodeCache = _dataCache.Get<INodeCache>();

            ICachedNodeInformation node;
            string rest;
            nodeCache.GetNode(path, out node, out rest);
            if (node == null)
            {
                throw new UnknownActionException();
            }

            var accessType = node.CheckRights(_currentUser);

            if (!accessType.HasValue)
            {
                accessType = NodeAccessType.Read;
            }
            else if (accessType.Value == NodeAccessType.NoAccess)
            {
                throw new UnknownActionException();
            }

            var handler = (INodeHandler) DependencyResolver.Resolve(node.HandlerType);

            string settings = node.Settings;

            object settingsObject = null;

            if (!string.IsNullOrWhiteSpace(settings) && handler.SettingsModel != null)
            {
                settingsObject = JsonConvert.DeserializeObject(settings, handler.SettingsModel);
            }

            handler.Initialize(node.NodeId, settingsObject, controller, node.Path, node.FullPath, accessType.Value);

            while (!string.IsNullOrWhiteSpace(rest))
            {
                var childSettings = handler.GetChildHandler(rest);
                if (childSettings == null)
                {
                    throw new UnknownActionException();
                }
                settings = null;
                handler = childSettings.Handler;
                rest = childSettings.Rest;
            }

            var handlerType = handler.GetType();

            var actionName = data["-action-"].ToString();

            var method = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(m => string.Compare(m.Name, actionName, StringComparison.InvariantCultureIgnoreCase) == 0);

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
                    if (parameterName == "settings")
                    {
                        if (settings == null)
                        {
                            arguments[i] = null;
                        }
                        else
                        {
                            if (parameterType == typeof (string))
                            {
                                arguments[i] = settings;
                            }
                            else
                            {
                                arguments[i] = JsonConvert.DeserializeObject(settings, parameterType);
                            }
                        }

                    }
                    else if (parameterName.StartsWith("raw") && parameterType == typeof(string) && dataLowered.TryGetValue(parameterName.Remove(0, 3), out parameter))
                    {
                        arguments[i] = parameter != null ? JsonConvert.SerializeObject(parameter) : null;
                    }

                    continue;
                }

                arguments[i] = JsonHelper.ResolveAndDeserializeObject(JsonConvert.SerializeObject(parameter), parameterType);
            }

            handler.Context = dataLowered;

            return method.Invoke(handler, arguments);
        }
    }
}
