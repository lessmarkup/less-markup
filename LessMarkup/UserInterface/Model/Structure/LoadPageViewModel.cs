/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Exceptions;
using LessMarkup.Interfaces.Structure;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class LoadPageViewModel
    {
        private readonly IDataCache _dataCache;

        public string Template { get; set; }
        public string TemplateId { get; set; }
        public object ViewData { get; set; }
        public string Title { get; set; }
        public bool IsStatic { get; set; }
        public string[] Stylesheets { get; set; }
        public string[] Scripts { get; set; }
        public string Path { get; set; }

        public List<PageBreadcrumbModel> Breadcrumbs { get; set; }
        public List<ToolbarButtonModel> ToolbarButtons { get; set; } 

        public LoadPageViewModel(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        private static string GetViewContents(string viewName, object model, System.Web.Mvc.Controller controller)
        {
            using (var stringWriter = new StringWriter(CultureInfo.CurrentCulture))
            {
                var viewData = new ViewDataDictionary(model);
                var view = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                var viewContext = new ViewContext(controller.ControllerContext, view.View, viewData, controller.TempData, stringWriter);
                view.View.Render(viewContext, stringWriter);
                return stringWriter.ToString();
            }
        }

        public void Initialize(string path, List<string> cachedTemplates, System.Web.Mvc.Controller controller)
        {
            var pageCache = _dataCache.Get<PageCache>();

            CachedPageInformation page;
            string rest;

            pageCache.GetPage(path, out page, out rest);

            if (page == null)
            {
                return;
            }

            var page1 = page;

            Breadcrumbs = new List<PageBreadcrumbModel>();

            while (page1.Parent != null)
            {
                page1 = page1.Parent;
                Breadcrumbs.Insert(0, new PageBreadcrumbModel { Text = page1.Title, Url = page1.FullPath });
            }

            var handler = (IPageHandler) DependencyResolver.Resolve(page.HandlerType);

            if (handler == null)
            {
                return;
            }

            var objectId = page.PageId;

            Title = page.Title;

            Path = page.FullPath;

            var settings = page.Settings;

            if (!string.IsNullOrWhiteSpace(rest))
            {
                for (;;)
                {
                    var childSettings = handler.GetChildHandler(rest);
                    if (childSettings == null || !childSettings.Id.HasValue)
                    {
                        throw new ObjectNotFoundException("Unknown path");
                    }
                    Breadcrumbs.Add(new PageBreadcrumbModel
                    {
                        Text = Title,
                        Url = Path
                    });
                    objectId = childSettings.Id.Value;
                    handler = childSettings.Handler;
                    Title = childSettings.Title;
                    Path += "/" + childSettings.Path;
                    settings = null;

                    if (string.IsNullOrWhiteSpace(childSettings.Rest))
                    {
                        break;
                    }

                    rest = childSettings.Rest;
                }
            }

            TemplateId = handler.TemplateId;
            ToolbarButtons = new List<ToolbarButtonModel>();

            if (cachedTemplates == null || !cachedTemplates.Contains(TemplateId))
            {
                var viewPath = PageCache.GetViewPath(handler.ViewType);
                Template = GetViewContents(viewPath, handler, controller);
            }

            Dictionary<string, string> settingsObject = null;
            if (!string.IsNullOrWhiteSpace(settings))
            {
                settingsObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(settings);
            }

            ViewData = handler.GetViewData(objectId, settingsObject);
            IsStatic = handler.IsStatic;
        }
    }
}
