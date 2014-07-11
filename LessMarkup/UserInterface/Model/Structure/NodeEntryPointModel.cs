/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.Engine.Configuration;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.NodeHandlers.Common;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NodeEntryPointModel
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly ISiteMapper _siteMapper;

        public string Title { get; set; }
        public string LogoImageUrl { get; set; }
        public string InitialData { get; set; }

        public NodeEntryPointModel(IDataCache dataCache, ICurrentUser currentUser, IEngineConfiguration engineConfiguration, ISiteMapper siteMapper)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
            _siteMapper = siteMapper;
            _engineConfiguration = engineConfiguration;
        }

        private NavigationBarModel CreateNavigationBarChild(ICachedNodeInformation node)
        {
            var model = new NavigationBarModel
            {
                Children = new List<NavigationBarModel>(),
                Title = node.Title,
                Url = node.FullPath
            };

            if (node.Children == null || node.HandlerType == typeof(FlatPageNodeHandler))
            {
                return model;
            }

            foreach (var child in node.Children)
            {
                if (!child.Visible || child.HandlerType == typeof(FlatPageNodeHandler))
                {
                    continue;
                }

                var accessType = child.CheckRights(_currentUser);

                if (!accessType.HasValue || accessType.Value == NodeAccessType.NoAccess)
                {
                    continue;
                }

                model.Children.Add(CreateNavigationBarChild(child));
            }

            return model;
        }

        public bool Initialize(string path, System.Web.Mvc.Controller controller)
        {
            var viewData = DependencyResolver.Resolve<LoadNodeViewModel>();
            string nodeLoadError = null;
            try
            {
                if (!viewData.Initialize(path, null, controller, true))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
                nodeLoadError = e.Message;
            }

            var nodeCache = _dataCache.Get<INodeCache>();

            var rootNode = nodeCache.RootNode;
            Title = rootNode.Title;

            bool hasLogin;
            bool hasSearch = false;
            bool hasTree = false;

            if (_siteMapper.SiteId.HasValue)
            {
                var siteConfiguration = _dataCache.Get<SiteConfigurationCache>();
                var adminLoginPage = siteConfiguration.AdminLoginPage;
                if (string.IsNullOrWhiteSpace(adminLoginPage))
                {
                    adminLoginPage = _engineConfiguration.AdminLoginPage;
                }
                hasLogin = siteConfiguration.HasUsers || string.IsNullOrWhiteSpace(adminLoginPage);
                hasSearch = siteConfiguration.HasSearch;
                hasTree = siteConfiguration.HasNavigationBar;
            }
            else
            {
                hasLogin = string.IsNullOrWhiteSpace(_engineConfiguration.AdminLoginPage);
            }

            List<NavigationBarModel> navigationTree = null;

            if (hasTree)
            {
                navigationTree = new List<NavigationBarModel>();

                foreach (var childNode in rootNode.Children)
                {
                    if (!childNode.Visible)
                    {
                        continue;
                    }

                    var accessType = childNode.CheckRights(_currentUser);

                    if (accessType.HasValue && accessType.Value == NodeAccessType.NoAccess)
                    {
                        continue;
                    }

                    navigationTree.Add(CreateNavigationBarChild(childNode));
                }
            }

            InitialData = JsonConvert.SerializeObject(new
            {
                RootPath = rootNode.FullPath,
                RootTitle = rootNode.Title,
                Path = path ?? "",
                HasLogin = hasLogin,
                HasSearch = hasSearch,
                ShowConfiguration = _currentUser.IsAdministrator,
                ConfigurationPath = Constants.NodePath.Configuration,
                ProfilePath = Constants.NodePath.Profile,
                UserLoggedIn = _currentUser.UserId.HasValue,
                UserName = _currentUser.Email ?? "",
                NavigationTree = navigationTree,
                TopMenu = new List<MenuItemModel>(),
                ViewData = viewData,
                NodeLoadError = nodeLoadError,
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
