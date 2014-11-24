/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.Engine.Helpers;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Exceptions;
using LessMarkup.Interfaces.Security;
using LessMarkup.UserInterface.Model.Common;
using LessMarkup.UserInterface.Model.RecordModel;
using LessMarkup.UserInterface.Model.User;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class JsonEntryPointModel
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private readonly IChangeTracker _changeTracker;
        private long? NodeId { get; set; }

        public JsonEntryPointModel(IDataCache dataCache, ICurrentUser currentUser, IChangeTracker changeTracker)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
            _changeTracker = changeTracker;
        }

        public static bool AppliesToRequest(HttpRequestBase request, string path)
        {
            return request.HttpMethod == "POST" && request.ContentType.StartsWith("application/json;");
        }

        private bool IsUserVerified()
        {
            return _currentUser.IsValidated && _currentUser.IsApproved;
        }

        private static Dictionary<string, object> GetRequestData()
        {
            HttpContext.Current.Request.InputStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream, HttpContext.Current.Request.ContentEncoding))
            {
                var requestText = reader.ReadToEnd();
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestText).ToDictionary(k => k.Key, v => v.Value);
                return data;
            }
        }

        public ActionResult HandleRequest(System.Web.Mvc.Controller controller)
        {
            var request = GetRequestData();

            object pathObj;
            if (!request.TryGetValue("path", out pathObj) || pathObj == null)
            {
                return new HttpNotFoundResult();
            }

            var path = pathObj.ToString();

            object commandObj;

            if (!request.TryGetValue("command", out commandObj) || commandObj == null)
            {
                return new HttpNotFoundResult();
            }

            var command = commandObj.ToString();

            if (string.IsNullOrEmpty(command))
            {
                return new HttpNotFoundResult();
            }

            var response = new Dictionary<string, object>();
            var userId = _currentUser.UserId;
            var userVerified = IsUserVerified();
            var isAdministrator = _currentUser.IsAdministrator;

            try
            {
                var resultData = HandleDataRequest(request, command, path, controller);

                object versionId;
                if (request.TryGetValue("versionId", out versionId))
                {
                    HandleUpdates(versionId != null ? Convert.ToInt64(versionId) : (long?)null, path, request, userId != _currentUser.UserId, response);
                }

                response["success"] = true;
                response["data"] = resultData;
            }
            catch (Exception e)
            {
                this.LogException(e);

                while (e.InnerException != null)
                {
                    e = e.InnerException;
                }

                response["success"] = false;
                response["message"] = e.Message;
            }

            response["loggedIn"] = _currentUser.UserId.HasValue;

            if (_currentUser.UserId.HasValue)
            {
                response["userName"] = _currentUser.Name;
            }

            if (IsUserVerified() != userVerified)
            {
                response["userNotVerified"] = !IsUserVerified();
            }

            if (isAdministrator != _currentUser.IsAdministrator)
            {
                response["showConfiguration"] = _currentUser.IsAdministrator;
            }

            return new ContentResult
            {
                ContentType = "application/json",
                Content = JsonConvert.SerializeObject(response)
            };
        }

        private void HandleUpdates(long? versionId, string path, Dictionary<string, object> arguments, bool userChanged, Dictionary<string, object> returnValues)
        {
            if (userChanged)
            {
                _changeTracker.Invalidate();
            }

            var changesCache = _dataCache.Get<IChangesCache>();
            var newVersionId = changesCache.LastChangeId;

            if (newVersionId.HasValue && newVersionId != versionId)
            {
                returnValues["versionId"] = newVersionId;
            }

            if (userChanged)
            {
                var notificationsModel = DependencyResolver.Resolve<UserInteraceElementsModel>();
                notificationsModel.Handle(returnValues, null, newVersionId);
            }

            if (newVersionId == versionId && !NodeId.HasValue)
            {
                return;
            }

            var model = DependencyResolver.Resolve<LoadUpdatesModel>();
            model.Handle(versionId, newVersionId, path, arguments, returnValues, NodeId);
        }

        private object HandleDataRequest(Dictionary<string, object> data, string command, string path, System.Web.Mvc.Controller controller)
        {
            switch (command)
            {
                case "form":
                {
                    var model = DependencyResolver.Resolve<InputFormDefinitionModel>();
                    model.Initialize(data["id"].ToString());
                    return model;
                }

                case "view":
                {
                    var model = DependencyResolver.Resolve<LoadNodeViewModel>();
                    if (!model.Initialize(data["newPath"].ToString(), JsonConvert.DeserializeObject<List<string>>(data["cached"].ToString()), controller, true, false))
                    {
                        throw new ObjectNotFoundException(LanguageHelper.GetText(Constants.ModuleType.UserInterface, UserInterfaceTextIds.UnknownPath));
                    }
                    if (model.NodeId.HasValue)
                    {
                        NodeId = model.NodeId;
                    }
                    return model;
                }

                case "loginStage1":
                {
                    var model = DependencyResolver.Resolve<LoginModel>();
                    return model.HandleStage1Request(data);
                }

                case "loginStage2":
                {
                    var model = DependencyResolver.Resolve<LoginModel>();
                    return model.HandleStage2Request(data);
                }

                case "idle":
                    return null;

                case "logout":
                {
                    var model = DependencyResolver.Resolve<LoginModel>();
                    return model.HandleLogout();
                }

                case "typeahead":
                {
                    var model = DependencyResolver.Resolve<TypeaheadModel>();
                    model.Initialize(path, data["property"].ToString(), data["searchText"].ToString());
                    return model;
                }

                case "getRegisterObject":
                {
                    var registerModel = DependencyResolver.Resolve<RegisterModel>();
                    return registerModel.GetRegisterObject();
                }

                case "register":
                {
                    var userProperties = data["user"].ToString();
                    var registerModel = (RegisterModel) JsonHelper.ResolveAndDeserializeObject(userProperties, typeof (RegisterModel));
                    return registerModel.Register(controller, userProperties);
                }

                case "searchText":
                {
                    var searchTextModel = DependencyResolver.Resolve<SearchTextModel>();
                    return searchTextModel.Handle(data["text"].ToString());
                }

                default:
                {
                    var model = DependencyResolver.Resolve<ExecuteActionModel>();
                    return model.HandleRequest(data, path);
                }
            }
        }
    }
}
