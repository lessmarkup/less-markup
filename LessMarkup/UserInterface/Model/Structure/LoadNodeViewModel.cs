/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using LessMarkup.Engine.HtmlTemplate;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class LoadNodeViewModel
    {
        private readonly IDataCache _dataCache;
        private INodeHandler _nodeHandler;
        private readonly ICurrentUser _currentUser;

        public string Template { get; set; }
        public string TemplateId { get; set; }
        public object ViewData { get; set; }
        public string Title { get; set; }
        public bool IsStatic { get; set; }
        public string[] Stylesheets { get; set; }
        public string[] Scripts { get; set; }
        public string Path { get; set; }

        internal INodeHandler NodeHandler { get { return _nodeHandler; } }

        public List<NodeBreadcrumbModel> Breadcrumbs { get; set; }
        public List<ToolbarButtonModel> ToolbarButtons { get; set; } 

        public LoadNodeViewModel(IDataCache dataCache, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
        }

        public static string GetViewPath(string viewName)
        {
            if (viewName.EndsWith("NodeHandler"))
            {
                viewName = viewName.Substring(0, viewName.Length - "NodeHandler".Length);
            }

            return "~/Views/" + viewName;
        }

        public static string GetViewContents(string viewName, object model, System.Web.Mvc.Controller controller)
        {
            using (var stringWriter = new StringWriter(CultureInfo.CurrentCulture))
            {
                var viewData = new ViewDataDictionary(model);
                var view = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                if (view.View == null)
                {
                    return null;
                }
                var viewContext = new ViewContext(controller.ControllerContext, view.View, viewData, controller.TempData, stringWriter);
                view.View.Render(viewContext, stringWriter);
                return stringWriter.ToString();
            }
        }

        public static string GetViewTemplate(INodeHandler handler, IDataCache dataCache, System.Web.Mvc.Controller controller)
        {
            var viewPath = GetViewPath(handler.ViewType);
            var templateCache = dataCache.Get<HtmlTemplateCache>();
            var template = templateCache.GetTemplate(viewPath + ".html") ?? GetViewContents(viewPath + ".cshtml", handler, controller);
            return template;
        }

        private void FillBreadcrumbs(CachedNodeInformation node)
        {
            if (node.Parent != null)
            {
                FillBreadcrumbs(node.Parent);
            }

            Breadcrumbs.Insert(0, new NodeBreadcrumbModel { Text = node.Title, Url = node.FullPath });
        }

        public bool Initialize(string path, List<string> cachedTemplates, System.Web.Mvc.Controller controller)
        {
            var nodeCache = _dataCache.Get<NodeCache>();

            CachedNodeInformation node;
            string rest;

            nodeCache.GetNode(path, out node, out rest);

            if (node == null)
            {
                return false;
            }

            if (node.LoggedIn && !_currentUser.UserId.HasValue)
            {
                return false;
            }

            var accessType = node.CheckRights(_currentUser);

            if (!accessType.HasValue)
            {
                accessType = NodeAccessType.Read;
            } 
            else if (accessType.Value == NodeAccessType.NoAccess)
            {
                return false;
            }

            Breadcrumbs = new List<NodeBreadcrumbModel>();

            if (node.Parent != null)
            {
                FillBreadcrumbs(node.Parent);
            }

            _nodeHandler = (INodeHandler) DependencyResolver.Resolve(node.HandlerType);

            if (_nodeHandler == null)
            {
                return false;
            }

            var objectId = node.NodeId;

            Title = node.Title;

            Path = node.FullPath;

            var settings = node.Settings;

            while (!string.IsNullOrWhiteSpace(rest))
            {
                var childSettings = _nodeHandler.GetChildHandler(rest);
                if (childSettings == null || !childSettings.Id.HasValue)
                {
                    return false;
                }
                Breadcrumbs.Add(new NodeBreadcrumbModel
                {
                    Text = Title,
                    Url = Path
                });
                objectId = childSettings.Id.Value;
                _nodeHandler = childSettings.Handler;
                Title = childSettings.Title;
                Path += "/" + childSettings.Path;
                settings = null;

                if (string.IsNullOrWhiteSpace(childSettings.Rest))
                {
                    break;
                }

                rest = childSettings.Rest;
            }

            TemplateId = _nodeHandler.TemplateId;
            ToolbarButtons = new List<ToolbarButtonModel>();

            if (cachedTemplates == null || !cachedTemplates.Contains(TemplateId))
            {
                Template = GetViewTemplate(_nodeHandler, _dataCache, controller);
                if (Template == null)
                {
                    return false;
                }
            }

            object settingsObject = null;
            if (!string.IsNullOrWhiteSpace(settings) && _nodeHandler.SettingsModel != null)
            {
                settingsObject = JsonConvert.DeserializeObject(settings, _nodeHandler.SettingsModel);
            }

            _nodeHandler.Initialize(objectId, settingsObject, controller, path, accessType.Value);
            ViewData = _nodeHandler.GetViewData();
            IsStatic = _nodeHandler.IsStatic;

            return true;
        }
    }
}
