/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using LessMarkup.Engine.FileSystem;
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
        public string Path { get; set; }
        public List<string> Require { get; set; }
        public ActionResult Result { get; set; }

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
            var stylesheets = handler.Stylesheets;
            if (stylesheets != null && stylesheets.Count > 0)
            {
                var resourceCache = dataCache.Get<ResourceCache>();
                var sb = new StringBuilder();
                sb.Append("<style scoped=\"scoped\">");
                foreach (var stylesheet in stylesheets)
                {
                    sb.Append(resourceCache.ReadText(stylesheet + ".css"));
                }
                sb.Append("</style>");
                template = sb + template;
            }
            return template;
        }

        private void FillBreadcrumbs(ICachedNodeInformation node)
        {
            if (node.Parent != null)
            {
                FillBreadcrumbs(node.Parent);
            }

            Breadcrumbs.Add(new NodeBreadcrumbModel { Text = node.Title, Url = node.FullPath });
        }

        public bool Initialize(string path, List<string> cachedTemplates, System.Web.Mvc.Controller controller, bool initializeUiElements, bool tryCreateResult)
        {
            path = HttpUtility.UrlDecode(path);

            var nodeCache = _dataCache.Get<INodeCache>();

            ICachedNodeInformation node;
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

            if (initializeUiElements)
            {
                Breadcrumbs = new List<NodeBreadcrumbModel>();

                if (node.Parent != null)
                {
                    FillBreadcrumbs(node.Parent);
                }
            }

            _nodeHandler = (INodeHandler) DependencyResolver.Resolve(node.HandlerType);

            if (_nodeHandler == null)
            {
                return false;
            }

            Title = node.Title;

            Path = node.FullPath;

            var settings = node.Settings;

            object settingsObject = null;

            if (!string.IsNullOrWhiteSpace(settings) && _nodeHandler.SettingsModel != null)
            {
                settingsObject = JsonConvert.DeserializeObject(settings, _nodeHandler.SettingsModel);
            }

            _nodeHandler.Initialize(node.NodeId, settingsObject, controller, node.Path, node.FullPath, accessType.Value);

            while (!string.IsNullOrWhiteSpace(rest))
            {
                var childSettings = _nodeHandler.GetChildHandler(rest);
                if (childSettings == null)
                {
                    return false;
                }

                if (initializeUiElements)
                {
                    Breadcrumbs.Add(new NodeBreadcrumbModel
                    {
                        Text = Title,
                        Url = Path
                    });
                }

                _nodeHandler = childSettings.Handler;
                Title = childSettings.Title;
                Path += "/" + childSettings.Path;

                if (string.IsNullOrWhiteSpace(childSettings.Rest))
                {
                    break;
                }

                rest = childSettings.Rest;
            }

            if (tryCreateResult)
            {
                Result = _nodeHandler.CreateResult();
                if (Result != null)
                {
                    return true;
                }
            }

            TemplateId = _nodeHandler.TemplateId;

            if (initializeUiElements)
            {
                ToolbarButtons = new List<ToolbarButtonModel>();

                if (cachedTemplates == null || !cachedTemplates.Contains(TemplateId))
                {
                    Template = GetViewTemplate(_nodeHandler, _dataCache, controller);
                    if (Template == null)
                    {
                        return false;
                    }
                }

                ViewData = _nodeHandler.GetViewData();
            }

            IsStatic = _nodeHandler.IsStatic;
            Require = _nodeHandler.Scripts;

            return true;
        }
    }
}
