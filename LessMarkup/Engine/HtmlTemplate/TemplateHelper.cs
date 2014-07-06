/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web;
using System.Web.Mvc;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Engine.HtmlTemplate
{
    public static class TemplateHelper
    {
        public static IHtmlString RenderTemplate(this HtmlHelper htmlHelper, string path)
        {
            var templateCache = Interfaces.DependencyResolver.Resolve<IDataCache>().Get<HtmlTemplateCache>();
            var template = templateCache.GetTemplate(path);
            return htmlHelper.Raw(template);
        }
    }
}
