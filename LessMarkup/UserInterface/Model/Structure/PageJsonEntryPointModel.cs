/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LessMarkup.Framework.FileSystem;
using LessMarkup.Interfaces.Cache;
using LessMarkup.UserInterface.Exceptions;
using LessMarkup.UserInterface.Model.RecordModel;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.DataFramework.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class PageJsonEntryPointModel
    {
        private readonly IDataCache _dataCache;

        public PageJsonEntryPointModel(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public static bool AppliesToRequest(HttpRequestBase request)
        {
            return request.HttpMethod == "POST" && request.ContentType.StartsWith("application/json;");
        }

        public ActionResult HandleRequest(System.Web.Mvc.Controller controller)
        {
            HttpContext.Current.Request.InputStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(HttpContext.Current.Request.InputStream, HttpContext.Current.Request.ContentEncoding))
            {
                var requestText = reader.ReadToEnd();
                var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestText).ToDictionary(k => k.Key, v => (string) v.Value.ToString());
                object result;
                try
                {
                    result = new
                    {
                        Success = true,
                        Data = HandleDataRequest(data, controller)
                    };
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                    {
                        e = e.InnerException;
                    }

                    result = new
                    {
                        Success = false,
                        e.Message
                    };
                }

                return new ContentResult
                {
                    ContentType = "application/json",
                    Content = JsonConvert.SerializeObject(result)
                };
            }
        }

        private object HandleDataRequest(Dictionary<string, string> data, System.Web.Mvc.Controller controller)
        {
            var command = data["-command-"];

            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            switch (command)
            {
                case "InputFormTemplate":
                {
                    var resourceCache = _dataCache.Get<ResourceCache>();
                    var templateBody = resourceCache.ReadText("~/Views/Structure/InputFormTemplate.cshtml");
                    return templateBody;
                }

                case "InputFormDefinition":
                {
                    var model = DependencyResolver.Resolve<InputFormDefinitionModel>();
                    model.Initialize(data["-id-"]);
                    return model;
                }

                case "View":
                {
                    var model = DependencyResolver.Resolve<LoadPageViewModel>();
                    model.Initialize(data["-path-"], JsonConvert.DeserializeObject<List<string>>(data["-cached-"]), controller);
                    return model;
                }

                case "Action":
                {
                    var model = DependencyResolver.Resolve<ExecuteActionModel>();
                    return model.HandleRequest(data);
                }

                case "LoginStage1":
                {
                    var model = DependencyResolver.Resolve<LoginModel>();
                    return model.HandleStage1Request(data);
                }

                case "LoginStage2":
                {
                    var model = DependencyResolver.Resolve<LoginModel>();
                    return model.HandleStage2Request(data);
                }

                case "Logout":
                {
                    var model = DependencyResolver.Resolve<LoginModel>();
                    return model.HandleLogout();
                }

                case "Typeahead":
                {
                    var model = DependencyResolver.Resolve<TypeaheadModel>();
                    model.Initialize(data["-path-"], data["property"], data["searchText"]);
                    return model;
                }

                default:
                    throw new UnknownActionException();
            }
        }
    }
}
