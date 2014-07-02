/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.Framework.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NodeEntryPointModel
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public string Title { get; set; }
        public string LogoImageUrl { get; set; }
        public string InitialData { get; set; }

        public NodeEntryPointModel(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public bool Initialize(string path, System.Web.Mvc.Controller controller)
        {
            var viewData = DependencyResolver.Resolve<LoadNodeViewModel>();
            string nodeLoadError = null;
            try
            {
                if (!viewData.Initialize(path, null, controller))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
                nodeLoadError = e.Message;
            }

            var rootNode = _dataCache.Get<NodeCache>().RootNode;
            Title = rootNode.Title;

            InitialData = JsonConvert.SerializeObject(new
            {
                RootPath = rootNode.FullPath,
                RootTitle = rootNode.Title,
                Path = path ?? "",
                HasLogin = true,
                HasSearch = false,
                ShowConfiguration = _currentUser.IsAdministrator,
                // TBD: calculate, now is true just to test functionality
                ConfigurationPath = "/configuration",
                ProfilePath = "/profile",
                UserLoggedIn = _currentUser.UserId.HasValue,
                UserName = _currentUser.Email ?? "",
                NavigationBar = new List<NavigationBarModel>(),
                TopMenu = new List<MenuItemModel>(),
                ViewData = viewData,
                NodeLoadError = nodeLoadError
            });

            return true;
        }

        public ActionResult CreateResult(System.Web.Mvc.Controller controller)
        {
            var result = new ViewResult();
            controller.ViewData.Model = this;
            result.ViewData = controller.ViewData;
            result.TempData = controller.TempData;
            result.ViewName = "~/Views/EntryPoint.cshtml";
            result.MasterName = null;
            result.ViewEngineCollection = controller.ViewEngineCollection;
            return result;
        }
    }
}
