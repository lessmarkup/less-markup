/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using System.Web.Mvc;
using LessMarkup.Framework.FileSystem;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Framework.Build.View
{
    public class CompiledViewEngine : RazorViewEngine
    {
        private readonly IDataCache _dataCache;
        private const string LayoutPath = null;

        public CompiledViewEngine(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public ConstructorInfo GetConstructor(string viewPath)
        {
            if (!viewPath.StartsWith("~/Views/"))
            {
                return null;
            }

            var type = _dataCache.Get<ResourceCache>().LoadType(viewPath.Substring(1));

            if (type == null)
            {
                return null;
            }

            return type.GetConstructor(Type.EmptyTypes);
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            var constructor = GetConstructor(viewPath);
            if (constructor != null)
            {
                return new CompiledView(controllerContext, viewPath, LayoutPath, true, FileExtensions, constructor, this);
            }

            return base.CreateView(controllerContext, viewPath, masterPath);
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            var constructor = GetConstructor(partialPath);
            if (constructor != null)
            {
                return new CompiledView(controllerContext, partialPath, LayoutPath, false, FileExtensions, constructor, this);
            }

            return base.CreatePartialView(controllerContext, partialPath);
        }

        protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
        {
            var constructor = GetConstructor(virtualPath);
            if (constructor != null)
            {
                return true;
            }
            return base.FileExists(controllerContext, virtualPath);
        }

        public override ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }

            if (string.IsNullOrEmpty(partialViewName))
            {
                throw new ArgumentException("Argument is null or empty", "partialViewName");
            }

            bool isAbsolutePath = partialViewName.StartsWith("~/");

            var controllerName = controllerContext.RouteData.GetRequiredString("controller");

            var viewPath = isAbsolutePath ? partialViewName : string.Format("~/Views/{0}/{1}.cshtml", controllerName, partialViewName);

            var constructor = GetConstructor(viewPath);

            if (constructor == null && !isAbsolutePath)
            {
                viewPath = string.Format("~/Views/Shared/{0}.cshtml", partialViewName);
                constructor = GetConstructor(viewPath);
            }

            if (constructor != null)
            {
                return new ViewEngineResult(new CompiledView(controllerContext, viewPath, LayoutPath, false, FileExtensions, constructor, this), this);
            }

            return base.FindPartialView(controllerContext, partialViewName, useCache);
        }

        public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }
            if (string.IsNullOrEmpty(viewName))
            {
                throw new ArgumentException("Argument is null or empty", "viewName");
            }

            bool isAbsolutePath = viewName.StartsWith("~/");

            var controllerName = controllerContext.RouteData.GetRequiredString("controller");

            var viewPath = isAbsolutePath ? viewName : string.Format("~/Views/{0}/{1}.cshtml", controllerName, viewName);

            var constructor = GetConstructor(viewPath);

            if (constructor == null && !isAbsolutePath)
            {
                viewPath = string.Format("~/Views/Shared/{0}.cshtml", viewName);
                constructor = GetConstructor(viewPath);
            }

            if (constructor != null)
            {
                var view = new CompiledView(controllerContext, viewPath, LayoutPath, true, FileExtensions, constructor, this);

                return new ViewEngineResult(view, this);
            }

            return base.FindView(controllerContext, viewName, masterName, useCache);
        }
    }
}
