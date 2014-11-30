/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class LoadNodeViewModel
    {
        private readonly IDataCache _dataCache;
        private INodeHandler _nodeHandler;
        public string Template { get; set; }
        public string TemplateId { get; set; }
        public object ViewData { get; set; }
        public string Title { get; set; }
        public bool IsStatic { get; set; }
        public string Path { get; set; }
        public List<string> Require { get; set; }
        public ActionResult Result { get; set; }

        internal INodeHandler NodeHandler { get { return _nodeHandler; } }
        internal long? NodeId { get; set; }

        public List<NodeBreadcrumbModel> Breadcrumbs { get; set; }
        public List<ToolbarButtonModel> ToolbarButtons { get; set; } 

        public LoadNodeViewModel(IDataCache dataCache)
        {
            _dataCache = dataCache;
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
            var resourceCache = dataCache.Get<IResourceCache>(dataCache.Get<ILanguageCache>().CurrentLanguageId);
            var template = resourceCache.ReadText(viewPath + ".html") ?? GetViewContents(viewPath + ".cshtml", handler, controller);
            var stylesheets = handler.Stylesheets;
            if (stylesheets != null && stylesheets.Count > 0)
            {
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

        public bool Initialize(string path, List<string> cachedTemplates, System.Web.Mvc.Controller controller, bool initializeUiElements, bool tryCreateResult)
        {
            this.LogDebug("Load view for path '" + path + "'");

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

            if (initializeUiElements)
            {
                Breadcrumbs = new List<NodeBreadcrumbModel>();
            }

            _nodeHandler = nodeCache.GetNodeHandler(path, controller, (nodeHandler, nodeTitle, nodePath, nodeRest, nodeId) =>
            {
                if (nodeId.HasValue)
                {
                    NodeId = nodeId;
                }

                if (initializeUiElements)
                {
                    Breadcrumbs.Add(new NodeBreadcrumbModel
                    {
                        Text = nodeTitle,
                        Url = nodePath
                    });
                }

                Title = nodeTitle;
                Path = nodePath;

                if (nodeHandler == null)
                {
                    return false;
                }

                Result = nodeHandler.CreateResult(nodeRest);

                return Result != null;
            });

            if (Result != null)
            {
                return true;
            }

            if (_nodeHandler != null)
            { 
                this.LogDebug("Found node " + (_nodeHandler.ObjectId.HasValue ? _nodeHandler.ObjectId.Value.ToString(CultureInfo.InvariantCulture) : "(no id)"));
            }

            if (initializeUiElements && Breadcrumbs.Count > 0)
            {
                Breadcrumbs.Remove(Breadcrumbs.Last());
            }

            if (_nodeHandler == null)
            {
                return false;
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

                this.LogDebug("BeginNodeHandlerGetViewData");

                ViewData = _nodeHandler.GetViewData();

                this.LogDebug("EndNodeHandlerGetViewData");
            }

            IsStatic = _nodeHandler.IsStatic;
            Require = _nodeHandler.Scripts;

            return true;
        }
    }
}
