/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;
using LessMarkup.Engine.Logging;
using LessMarkup.Framework.Helpers;

namespace LessMarkup.Engine.Build.View
{
    public class CompiledView : RazorView, IView
    {
        private class PathFactory : IVirtualPathFactory
        {
            private readonly IVirtualPathFactory _parent;
            private readonly CompiledViewEngine _viewEngine;

            public PathFactory(IVirtualPathFactory parent, CompiledViewEngine viewEngine)
            {
                _parent = parent;
                _viewEngine = viewEngine;
            }

            public bool Exists(string virtualPath)
            {
                if (_parent.Exists(virtualPath))
                {
                    return true;
                }

                return _viewEngine.GetConstructor(virtualPath) != null;
            }

            public object CreateInstance(string virtualPath)
            {
                var ret =  _parent.CreateInstance(virtualPath);
                if (ret != null)
                {
                    return ret;
                }

                ret = _viewEngine.GetConstructor(virtualPath).Invoke(null);
                return ret;
            }
        }

        private readonly ConstructorInfo _constructor;
        private readonly CompiledViewEngine _viewEngine;

        public CompiledView(ControllerContext controllerContext, string viewPath, string layoutPath, bool runViewStartPages, IEnumerable<string> viewStartFileExtensions, ConstructorInfo constructor, CompiledViewEngine viewEngine) : base(controllerContext, viewPath, layoutPath, runViewStartPages, viewStartFileExtensions)
        {
            _constructor = constructor;
            _viewEngine = viewEngine;
        }

        private void OnRenderPage()
        {
        }

        void IView.Render(ViewContext viewContext, TextWriter writer)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException("viewContext");
            }
            var obj = _constructor.Invoke(null);
            if (obj == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Cannot create view from path {0}", ViewPath));
            }
            var page = obj as WebViewPage;
            if (page != null && !(page.VirtualPathFactory is PathFactory))
            {
                page.VirtualPathFactory = new PathFactory(page.VirtualPathFactory, _viewEngine);
            }

            if (page != null)
            {
                this.LogDebug("Started rendering page '" + ViewPath + "'");
                OnRenderPage();
            }

            var startTime = Environment.TickCount;

            try
            {
                RenderView(viewContext, writer, obj);
            }
            catch (Exception e)
            {
                this.LogException(e);
                throw;
            }

            if (page != null)
            {
                this.LogDebug("Finished rendering page '" + ViewPath + "', took " + (Environment.TickCount-startTime) + " ms");
            }
        }
    }
}
