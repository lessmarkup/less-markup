/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.Framework.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.DataFramework.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class PageEntryPointModel
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;

        public string Title { get; set; }
        public string LogoImageUrl { get; set; }
        public string InitialData { get; set; }

        public PageEntryPointModel(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public bool Initialize(string path, System.Web.Mvc.Controller controller)
        {
            var pageCache = _dataCache.Get<PageCache>();

            CachedPageInformation cachedPage;
            string rest;
            pageCache.GetPage(path, out cachedPage, out rest);
            if (cachedPage == null)
            {
                return false;
            }

            var viewData = DependencyResolver.Resolve<LoadPageViewModel>();
            string pageLoadError = null;
            try
            {
                viewData.Initialize(path, null, controller);
            }
            catch (Exception e)
            {
                this.LogException(e);
                pageLoadError = e.Message;
            }

            Title = cachedPage.Root.Title;

            InitialData = JsonConvert.SerializeObject(new
            {
                RootPath = cachedPage.Root.FullPath,
                RootTitle = cachedPage.Root.Title,
                Path = path,
                HasLogin = true,
                HasSearch = false,
                ShowConfiguration = _currentUser.IsAdministrator,
                // TBD: calculate, now is true just to test functionality
                ConfigurationPath = "/page/configuration",
                ProfilePath = "/page/profile",
                UserLoggedIn = _currentUser.UserId.HasValue,
                UserName = _currentUser.Email ?? "",
                NavigationBar = new List<NavigationBarModel>(),
                TopMenu = new List<MenuItemModel>(),
                Copyright = "Copyright(c) MvcDesign",
                ViewData = viewData,
                PageLoadError = pageLoadError
            });

            return true;
        }

        public ActionResult CreateResult(System.Web.Mvc.Controller controller)
        {
            var result = new ViewResult();
            controller.ViewData.Model = this;
            result.ViewData = controller.ViewData;
            result.TempData = controller.TempData;
            result.ViewName = "~/Views/Structure/EntryPoint.cshtml";
            result.MasterName = null;
            result.ViewEngineCollection = controller.ViewEngineCollection;
            return result;
        }
    }
}
